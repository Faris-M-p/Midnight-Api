using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MidnightApi.Options;
using Npgsql;

namespace MidnightApi.DataAccess;

/// <summary>
/// Development-only database call tracer. Must never throw into the request pipeline.
/// </summary>
public sealed class DatabaseTraceWriter
{
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "confirmpassword",
        "confirm_password",
        "p_password",
        "p_confirmpassword",
        "token",
        "accesstoken",
        "access_token",
        "refreshtoken",
        "refresh_token",
        "jwt",
        "authorization",
        "secret",
        "apikey",
        "api_key"
    };

    private readonly object _gate = new();
    private readonly bool _enabled;
    private readonly string _logPath;

    public DatabaseTraceWriter(IOptions<DatabaseTraceOptions> options, IHostEnvironment environment)
    {
        var enabled = options.Value.Enabled;
        _logPath = Path.Combine(environment.ContentRootPath, "DatabaseTrace.log");

        if (!enabled)
        {
            _enabled = false;
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                _logPath,
                $"# DatabaseTrace session started {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}" +
                $"# File: {_logPath}{Environment.NewLine}" +
                $"# Enabled=true — every PostgreSQL call through DataAccessDapper is traced here.{Environment.NewLine}" +
                $"{Environment.NewLine}");
            _enabled = true;
        }
        catch
        {
            // Tracing must never prevent API startup.
            _enabled = false;
        }
    }

    public bool Enabled => _enabled;

    public DatabaseTraceEntry? TryBegin(
        string storedProcedureName,
        object? parameter,
        CallModeExtra extras,
        string? fetchCursorName,
        HttpContext? httpContext)
    {
        if (!_enabled)
        {
            return null;
        }

        try
        {
            return new DatabaseTraceEntry
            {
                Timestamp = DateTime.Now,
                ProcedureName = storedProcedureName,
                Caller = ResolveCaller(),
                RequestInfo = ResolveRequestInfo(httpContext),
                Parameters = BuildParameterList(parameter, extras),
                ExecutableSql = BuildExecutableSql(storedProcedureName, parameter, extras, fetchCursorName)
            };
        }
        catch
        {
            return null;
        }
    }

    public void TryComplete(DatabaseTraceEntry? entry, Stopwatch? sw, bool success, Exception? error = null)
    {
        if (!_enabled || entry is null)
        {
            return;
        }

        try
        {
            sw?.Stop();
            var completed = new DatabaseTraceEntry
            {
                Timestamp = entry.Timestamp,
                ProcedureName = entry.ProcedureName,
                Caller = entry.Caller,
                RequestInfo = entry.RequestInfo,
                Parameters = entry.Parameters,
                ExecutableSql = entry.ExecutableSql,
                ElapsedMilliseconds = sw?.ElapsedMilliseconds ?? 0
            };

            Write(completed, success, error);
        }
        catch
        {
            // Never affect the caller.
        }
    }

    private void Write(DatabaseTraceEntry entry, bool success, Exception? error)
    {
        var builder = new StringBuilder();
        builder.AppendLine("================================================================================");
        builder.AppendLine($"Timestamp     : {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
        builder.AppendLine($"Status        : {(success ? "Success" : "Failed")}");
        builder.AppendLine($"Elapsed       : {entry.ElapsedMilliseconds} ms");
        builder.AppendLine($"Procedure     : {entry.ProcedureName}");
        builder.AppendLine($"Caller        : {entry.Caller}");
        builder.AppendLine($"Request       : {entry.RequestInfo}");
        builder.AppendLine("Parameters    :");

        if (entry.Parameters.Count == 0)
        {
            builder.AppendLine("  (none)");
        }
        else
        {
            foreach (var parameter in entry.Parameters)
            {
                builder.AppendLine($"  {parameter.Key} = {parameter.Value}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("-- Executable PostgreSQL");
        builder.AppendLine(entry.ExecutableSql);

        if (!success && error is not null)
        {
            builder.AppendLine();
            builder.AppendLine("PostgreSQL Error:");
            builder.AppendLine($"  Type    : {error.GetType().FullName}");
            builder.AppendLine($"  Message : {error.Message}");

            if (error is PostgresException pg)
            {
                builder.AppendLine($"  SqlState: {pg.SqlState}");
                builder.AppendLine($"  Detail  : {pg.Detail}");
                builder.AppendLine($"  Hint    : {pg.Hint}");
                builder.AppendLine($"  Where   : {pg.Where}");
            }

            if (error.InnerException is not null)
            {
                builder.AppendLine($"  Inner   : {error.InnerException.Message}");
            }
        }

        builder.AppendLine();

        lock (_gate)
        {
            File.AppendAllText(_logPath, builder.ToString());
        }
    }

    private static string ResolveCaller()
    {
        var stack = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
        foreach (var frame in stack.GetFrames() ?? Array.Empty<StackFrame>())
        {
            var method = frame.GetMethod();
            var declaringType = method?.DeclaringType;
            if (method is null || declaringType is null)
            {
                continue;
            }

            var fullName = declaringType.FullName ?? string.Empty;
            if (fullName.StartsWith("MidnightApi.DataAccess", StringComparison.Ordinal) ||
                fullName.StartsWith("System.", StringComparison.Ordinal) ||
                fullName.StartsWith("Microsoft.", StringComparison.Ordinal))
            {
                continue;
            }

            return $"{declaringType.FullName}.{method.Name}";
        }

        return "(unknown caller)";
    }

    private static string ResolveRequestInfo(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return "(no HTTP request)";
        }

        var request = httpContext.Request;
        return $"{request.Method} {request.Path}{request.QueryString} | TraceId={httpContext.TraceIdentifier}";
    }

    private static List<KeyValuePair<string, string>> BuildParameterList(object? parameter, CallModeExtra extras)
    {
        var list = new List<KeyValuePair<string, string>>();

        if (parameter is not null)
        {
            foreach (var property in parameter.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                var dbName = property.GetCustomAttribute<DbParamAttribute>()?.Name ?? property.Name;
                list.Add(new KeyValuePair<string, string>(dbName, FormatDisplayValue(dbName, property.GetValue(parameter))));
            }
        }

        foreach (var extra in extras.Items)
        {
            list.Add(new KeyValuePair<string, string>(extra.Key, FormatDisplayValue(extra.Key, extra.Value)));
        }

        return list;
    }

    private static string BuildExecutableSql(
        string storedProcedureName,
        object? parameter,
        CallModeExtra extras,
        string? fetchCursorName)
    {
        var args = new List<string>();

        if (parameter is not null)
        {
            foreach (var property in parameter.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead)
                {
                    continue;
                }

                var dbName = property.GetCustomAttribute<DbParamAttribute>()?.Name ?? property.Name;
                args.Add($"{QuoteIdent(dbName)} := {FormatSqlLiteral(dbName, property.GetValue(parameter))}");
            }
        }

        foreach (var extra in extras.Items)
        {
            args.Add($"{QuoteIdent(extra.Key)} := {FormatSqlLiteral(extra.Key, extra.Value)}");
        }

        var call = args.Count == 0
            ? $@"CALL ""{storedProcedureName}""();"
            : $@"CALL ""{storedProcedureName}""({string.Join(", ", args)});";

        if (string.IsNullOrWhiteSpace(fetchCursorName))
        {
            return call;
        }

        return
            $"BEGIN;{Environment.NewLine}" +
            $"{call}{Environment.NewLine}" +
            $"FETCH ALL FROM \"{fetchCursorName.Trim().Trim('"')}\";{Environment.NewLine}" +
            "COMMIT;";
    }

    private static string FormatDisplayValue(string name, object? value)
    {
        if (IsSensitive(name))
        {
            return "***";
        }

        return value switch
        {
            null => "NULL",
            string s when LooksLikeJwt(s) => "***",
            string s => s,
            bool b => b ? "true" : "false",
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            byte[] => "(byte[])",
            IEnumerable enumerable when value is not string => FormatEnumerable(enumerable),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "NULL"
        };
    }

    private static string FormatSqlLiteral(string name, object? value)
    {
        if (IsSensitive(name))
        {
            return "'***'";
        }

        if (value is null)
        {
            return "NULL";
        }

        if (value is string text)
        {
            if (LooksLikeJwt(text))
            {
                return "'***'";
            }

            if (LooksLikeJson(text))
            {
                return $"'{EscapeLiteral(text)}'::jsonb";
            }

            return $"'{EscapeLiteral(text)}'";
        }

        return value switch
        {
            bool b => b ? "true" : "false",
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal =>
                Convert.ToString(value, CultureInfo.InvariantCulture)!,
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.ffffff}'",
            DateTimeOffset dto => $"'{dto.UtcDateTime:yyyy-MM-dd HH:mm:ss.ffffff}'",
            DateOnly d => $"'{d:yyyy-MM-dd}'",
            Guid guid => $"'{guid}'",
            byte[] => "NULL",
            JsonDocument doc => $"'{EscapeLiteral(doc.RootElement.GetRawText())}'::jsonb",
            JsonElement el => $"'{EscapeLiteral(el.GetRawText())}'::jsonb",
            IEnumerable enumerable when value is not string =>
                $"'{EscapeLiteral(FormatEnumerable(enumerable))}'",
            _ => $"'{EscapeLiteral(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}'"
        };
    }

    private static string FormatEnumerable(IEnumerable enumerable)
    {
        var parts = new List<string>();
        foreach (var item in enumerable)
        {
            parts.Add(item?.ToString() ?? "null");
            if (parts.Count >= 50)
            {
                parts.Add("...");
                break;
            }
        }

        return $"[{string.Join(", ", parts)}]";
    }

    private static bool IsSensitive(string name)
    {
        var normalized = Regex.Replace(name ?? string.Empty, "[^A-Za-z0-9]", string.Empty);
        return SensitiveNames.Contains(normalized) ||
               normalized.Contains("password", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("token", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("jwt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeJwt(string value)
    {
        var parts = value.Split('.');
        return parts.Length == 3 &&
               parts.All(part => part.Length > 0) &&
               value.StartsWith("eyJ", StringComparison.Ordinal);
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.AsSpan().TrimStart();
        return trimmed.Length > 0 && (trimmed[0] is '{' or '[');
    }

    private static string EscapeLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string QuoteIdent(string name)
    {
        var trimmed = name.Trim().Trim('"');
        return $"\"{trimmed}\"";
    }
}

public sealed class DatabaseTraceEntry
{
    public DateTime Timestamp { get; init; }
    public string ProcedureName { get; init; } = string.Empty;
    public string Caller { get; init; } = string.Empty;
    public string RequestInfo { get; init; } = string.Empty;
    public long ElapsedMilliseconds { get; init; }
    public List<KeyValuePair<string, string>> Parameters { get; init; } = [];
    public string ExecutableSql { get; init; } = string.Empty;
}

public sealed class CallModeExtra
{
    public List<KeyValuePair<string, object?>> Items { get; } = [];

    public static CallModeExtra ForWrite() =>
        new CallModeExtra()
            .Add("p_ResponseCode", 0L)
            .Add("p_Status", false)
            .Add("p_ResponseMessage", string.Empty)
            .Add("p_Data", null);

    public static CallModeExtra ForJsonData() =>
        new CallModeExtra().Add("p_Data", null);

    public static CallModeExtra ForCursor(string cursorName) =>
        new CallModeExtra().Add("p_Result", cursorName);

    public static CallModeExtra ForPagedCursor(string cursorName) =>
        new CallModeExtra()
            .Add("p_Result", cursorName)
            .Add("p_TotalCount", 0);

    public CallModeExtra Add(string name, object? value)
    {
        Items.Add(new KeyValuePair<string, object?>(name, value));
        return this;
    }
}
