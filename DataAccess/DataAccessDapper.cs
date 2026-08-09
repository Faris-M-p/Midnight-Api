using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;
using MidnightApi.Models;
using Npgsql;
using NpgsqlTypes;

namespace MidnightApi.DataAccess;

public sealed class DataAccessDapper : IDataAccessDapper
{
    private const string ResponseCodeParam = "p_ResponseCode";
    private const string StatusParam = "p_Status";
    private const string ResponseMessageParam = "p_ResponseMessage";
    private const string DataParam = "p_Data";
    private const string ResultParam = "p_Result";
    private const string TotalCountParam = "p_TotalCount";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly string _connectionString;
    private readonly ILogger<DataAccessDapper> _logger;

    public DataAccessDapper(IConfiguration configuration, ILogger<DataAccessDapper> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.");
        _logger = logger;
    }

    public async Task<List<TResult>> GetListByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var (items, _) = await FetchCursorAsync<TResult>(storedProcedureName, parameter, includeTotalCount: false);
        return items;
    }

    public Task<(List<TResult> Items, int TotalCount)> GetPagedListByStoredProcedureAsync<TResult>(
        string storedProcedureName,
        object? parameter = null) =>
        FetchCursorAsync<TResult>(storedProcedureName, parameter, includeTotalCount: true);

    public async Task<TResult?> GetSingleOrDefaultByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var (items, _) = await FetchCursorAsync<TResult>(storedProcedureName, parameter, includeTotalCount: false);
        return items.Count == 0 ? default : items[0];
    }

    public async Task<TResult> GetSingleByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        if (IsCommonResponse(typeof(TResult)))
        {
            return await CallWriteAsync<TResult>(storedProcedureName, parameter);
        }

        var payload = await CallJsonDataAsync(storedProcedureName, parameter)
            ?? throw new InvalidOperationException($"Procedure '{storedProcedureName}' returned no payload.");
        return FromJson<TResult>(payload)
            ?? throw new InvalidOperationException($"Procedure '{storedProcedureName}' returned an empty payload.");
    }

    public async Task<TResult?> GetPayloadByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var payload = await CallJsonDataAsync(storedProcedureName, parameter);
        return FromJson<TResult>(payload);
    }

    public async Task<TResult?> ExecuteScalarByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var (items, _) = await FetchCursorAsync<TResult>(storedProcedureName, parameter, includeTotalCount: false);
        return items.Count == 0 ? default : items[0];
    }

    public async Task<int> ExecuteByStoredProcedureAsync(string storedProcedureName, object? parameter = null)
    {
        await CallWriteAsync<CommonResponse<IdResponse>>(storedProcedureName, parameter);
        return 0;
    }

    private async Task<TResult> CallWriteAsync<TResult>(string storedProcedureName, object? parameter)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = CreateCallCommand(connection, storedProcedureName, parameter, CallMode.Write);
            await cmd.ExecuteNonQueryAsync();

            var result = Activator.CreateInstance<TResult>()!;
            ApplyWriteResult(result, cmd);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stored procedure command failed.");
            throw;
        }
    }

    private async Task<object?> CallJsonDataAsync(string storedProcedureName, object? parameter)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = CreateCallCommand(connection, storedProcedureName, parameter, CallMode.JsonData);
            await cmd.ExecuteNonQueryAsync();
            return GetParameterValue(cmd, DataParam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stored procedure command failed.");
            throw;
        }
    }

    private async Task<(List<TResult> Items, int TotalCount)> FetchCursorAsync<TResult>(
        string storedProcedureName,
        object? parameter,
        bool includeTotalCount)
    {
        var cursorName = "c" + Guid.NewGuid().ToString("N");

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await using var cmd = CreateCallCommand(
                connection,
                storedProcedureName,
                parameter,
                includeTotalCount ? CallMode.PagedCursor : CallMode.Cursor,
                cursorName);
            cmd.Transaction = transaction;
            await cmd.ExecuteNonQueryAsync();

            var openedCursor = Convert.ToString(GetParameterValue(cmd, ResultParam)) ?? cursorName;
            var items = (await connection.QueryAsync<TResult>(
                $"FETCH ALL FROM {openedCursor};",
                transaction: transaction)).AsList();

            var totalCount = includeTotalCount
                ? Convert.ToInt32(GetParameterValue(cmd, TotalCountParam) ?? 0)
                : items.Count;

            await transaction.CommitAsync();
            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stored procedure command failed.");
            throw;
        }
    }

    private static NpgsqlCommand CreateCallCommand(
        NpgsqlConnection connection,
        string storedProcedureName,
        object? parameter,
        CallMode mode,
        string? cursorName = null)
    {
        var sql = BuildCallSql(storedProcedureName, parameter, mode);
        var cmd = new NpgsqlCommand(sql, connection);

        if (parameter is not null)
        {
            foreach (var property in GetInputProperties(parameter))
            {
                cmd.Parameters.Add(CreateInputParameter(property.Name, property.GetValue(parameter)));
            }
        }

        switch (mode)
        {
            case CallMode.Write:
                cmd.Parameters.Add(new NpgsqlParameter(ResponseCodeParam, NpgsqlDbType.Bigint)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = 0L
                });
                cmd.Parameters.Add(new NpgsqlParameter(StatusParam, NpgsqlDbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                });
                cmd.Parameters.Add(new NpgsqlParameter(ResponseMessageParam, NpgsqlDbType.Varchar)
                {
                    Direction = ParameterDirection.InputOutput,
                    Size = 4000,
                    Value = string.Empty
                });
                cmd.Parameters.Add(new NpgsqlParameter(DataParam, NpgsqlDbType.Jsonb)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                });
                break;
            case CallMode.JsonData:
                cmd.Parameters.Add(new NpgsqlParameter(DataParam, NpgsqlDbType.Jsonb)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = DBNull.Value
                });
                break;
            case CallMode.Cursor:
            case CallMode.PagedCursor:
                cmd.Parameters.Add(new NpgsqlParameter(ResultParam, NpgsqlDbType.Refcursor)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = cursorName ?? "result_cursor"
                });
                if (mode == CallMode.PagedCursor)
                {
                    cmd.Parameters.Add(new NpgsqlParameter(TotalCountParam, NpgsqlDbType.Integer)
                    {
                        Direction = ParameterDirection.InputOutput,
                        Value = 0
                    });
                }

                break;
        }

        return cmd;
    }

    private static NpgsqlParameter CreateInputParameter(string name, object? value)
    {
        if (value is string text)
        {
            // Npgsql defaults strings to `text`. Postgres CALL matching is strict,
            // so VARCHAR args (e.g. ProAccountLogin.p_Username) will not bind.
            if (LooksLikeJson(text))
            {
                return new NpgsqlParameter(name, NpgsqlDbType.Jsonb) { Value = text };
            }

            return new NpgsqlParameter(name, NpgsqlDbType.Varchar) { Value = text };
        }

        return new NpgsqlParameter(name, value ?? DBNull.Value);
    }

    private static void ApplyWriteResult<TResult>(TResult result, NpgsqlCommand cmd)
    {
        ArgumentNullException.ThrowIfNull(result);
        var type = typeof(TResult);
        SetProperty(result, type, "ResponseCode", Convert.ToInt64(GetParameterValue(cmd, ResponseCodeParam) ?? 0L));
        SetProperty(result, type, "Status", Convert.ToBoolean(GetParameterValue(cmd, StatusParam) ?? false));
        SetProperty(result, type, "ResponseMessage", Convert.ToString(GetParameterValue(cmd, ResponseMessageParam)) ?? string.Empty);

        var dataProperty = type.GetProperty("Data", BindingFlags.Instance | BindingFlags.Public);
        if (dataProperty is null || !dataProperty.CanWrite)
        {
            return;
        }

        dataProperty.SetValue(result, FromJson(dataProperty.PropertyType, GetParameterValue(cmd, DataParam)));
    }

    private static object? GetParameterValue(NpgsqlCommand cmd, string name)
    {
        var value = cmd.Parameters[name].Value;
        return value is DBNull ? null : value;
    }

    private static void SetProperty(object target, Type type, string name, object? value)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        property?.SetValue(target, value);
    }

    private static bool IsCommonResponse(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(CommonResponse<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.AsSpan().TrimStart();
        return trimmed.Length > 0 && (trimmed[0] is '{' or '[');
    }

    private static string BuildCallSql(string storedProcedureName, object? parameter, CallMode mode)
    {
        var args = new List<string>();

        if (parameter is not null)
        {
            args.AddRange(GetInputProperties(parameter).Select(property =>
            {
                var dbName = property.GetCustomAttribute<DbParamAttribute>()?.Name ?? property.Name;
                return $"{QuoteIdent(dbName)} := @{property.Name}";
            }));
        }

        switch (mode)
        {
            case CallMode.Write:
                args.Add($"{QuoteIdent(ResponseCodeParam)} := @{ResponseCodeParam}");
                args.Add($"{QuoteIdent(StatusParam)} := @{StatusParam}");
                args.Add($"{QuoteIdent(ResponseMessageParam)} := @{ResponseMessageParam}");
                args.Add($"{QuoteIdent(DataParam)} := @{DataParam}");
                break;
            case CallMode.JsonData:
                args.Add($"{QuoteIdent(DataParam)} := @{DataParam}");
                break;
            case CallMode.Cursor:
                args.Add($"{QuoteIdent(ResultParam)} := @{ResultParam}");
                break;
            case CallMode.PagedCursor:
                args.Add($"{QuoteIdent(ResultParam)} := @{ResultParam}");
                args.Add($"{QuoteIdent(TotalCountParam)} := @{TotalCountParam}");
                break;
        }

        return args.Count == 0
            ? $@"CALL ""{storedProcedureName}""()"
            : $@"CALL ""{storedProcedureName}""({string.Join(", ", args)})";
    }

    private static string QuoteIdent(string name)
    {
        var trimmed = name.Trim().Trim('"');
        return $"\"{trimmed}\"";
    }

    private static PropertyInfo[] GetInputProperties(object parameter) =>
        parameter.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToArray();

    private static T? FromJson<T>(object? value) =>
        (T?)FromJson(typeof(T), value);

    private static object? FromJson(Type type, object? value)
    {
        if (value is null or DBNull)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        return value switch
        {
            string s when string.IsNullOrWhiteSpace(s) || s == "null" => type.IsValueType ? Activator.CreateInstance(type) : null,
            string s => JsonSerializer.Deserialize(s, type, JsonOptions),
            JsonDocument doc => doc.RootElement.Deserialize(type, JsonOptions),
            JsonElement el => el.Deserialize(type, JsonOptions),
            _ when type.IsInstanceOfType(value) => value,
            _ => JsonSerializer.Deserialize(value.ToString()!, type, JsonOptions)
        };
    }

    private enum CallMode
    {
        Write,
        JsonData,
        Cursor,
        PagedCursor
    }
}
