using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;
using MidnightApi.Models;
using Npgsql;

namespace MidnightApi.DataAccess;

public sealed class DataAccessDapper : IDataAccessDapper
{
    private const string ResponseCodeParam = "p_response_code";
    private const string StatusCodeParam = "p_status_code";
    private const string ResponseMessageParam = "p_response_message";
    private const string PayloadParam = "p_payload";

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
        var payload = await CallPayloadAsync(storedProcedureName, parameter);
        return FromJson<List<TResult>>(payload) ?? [];
    }

    public async Task<TResult?> GetSingleOrDefaultByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var payload = await CallPayloadAsync(storedProcedureName, parameter);
        return FromJson<TResult>(payload);
    }

    public async Task<TResult> GetSingleByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        if (typeof(CommonResponse).IsAssignableFrom(typeof(TResult)))
        {
            return await CallCommonResponseAsync<TResult>(storedProcedureName, parameter);
        }

        var payload = await CallPayloadAsync(storedProcedureName, parameter)
            ?? throw new InvalidOperationException($"Procedure '{storedProcedureName}' returned no payload.");
        return FromJson<TResult>(payload)
            ?? throw new InvalidOperationException($"Procedure '{storedProcedureName}' returned an empty payload.");
    }

    public async Task<TResult?> GetPayloadByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var payload = await CallPayloadAsync(storedProcedureName, parameter);
        return FromJson<TResult>(payload);
    }

    public async Task<TResult?> ExecuteScalarByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var payload = await CallPayloadAsync(storedProcedureName, parameter);
        return FromJson<TResult>(payload);
    }

    public async Task<int> ExecuteByStoredProcedureAsync(string storedProcedureName, object? parameter = null)
    {
        await CallPayloadAsync(storedProcedureName, parameter);
        return 0;
    }

    private async Task<TResult> CallCommonResponseAsync<TResult>(string storedProcedureName, object? parameter)
    {
        var dp = BuildParameters(parameter);
        dp.Add(ResponseCodeParam, 0, DbType.Int32, ParameterDirection.InputOutput);
        dp.Add(StatusCodeParam, 0, DbType.Int32, ParameterDirection.InputOutput);
        dp.Add(ResponseMessageParam, string.Empty, DbType.String, ParameterDirection.InputOutput, size: 4000);

        var sql = BuildCallSql(storedProcedureName, parameter, includeResponse: true, includePayload: false);
        await RunAsync(sql, conn => conn.ExecuteAsync(sql, dp));

        var result = (CommonResponse)(object)Activator.CreateInstance<TResult>()!;
        result.ResponseCode = dp.Get<int>(ResponseCodeParam);
        result.StatusCode = dp.Get<int>(StatusCodeParam);
        result.ResponseMessage = dp.Get<string>(ResponseMessageParam) ?? string.Empty;
        return (TResult)(object)result;
    }

    private async Task<object?> CallPayloadAsync(string storedProcedureName, object? parameter)
    {
        var dp = BuildParameters(parameter);
        dp.Add(PayloadParam, null, DbType.String, ParameterDirection.InputOutput, size: -1);

        var sql = BuildCallSql(storedProcedureName, parameter, includeResponse: false, includePayload: true);
        await RunAsync(sql, conn => conn.ExecuteAsync(sql, dp));
        return dp.Get<object>(PayloadParam);
    }

    private static DynamicParameters BuildParameters(object? parameter)
    {
        var dp = new DynamicParameters();
        if (parameter is null)
        {
            return dp;
        }

        foreach (var property in GetInputProperties(parameter))
        {
            dp.Add(property.Name, property.GetValue(parameter));
        }

        return dp;
    }

    private static string BuildCallSql(
        string storedProcedureName,
        object? parameter,
        bool includeResponse,
        bool includePayload)
    {
        var args = new List<string>();

        if (parameter is not null)
        {
            args.AddRange(GetInputProperties(parameter).Select(property =>
            {
                var dbName = property.GetCustomAttribute<DbParamAttribute>()?.Name ?? property.Name;
                return $"{dbName} := @{property.Name}";
            }));
        }

        if (includeResponse)
        {
            args.Add($"{ResponseCodeParam} := @{ResponseCodeParam}");
            args.Add($"{StatusCodeParam} := @{StatusCodeParam}");
            args.Add($"{ResponseMessageParam} := @{ResponseMessageParam}");
        }

        if (includePayload)
        {
            args.Add($"{PayloadParam} := @{PayloadParam}");
        }

        return args.Count == 0
            ? $@"CALL ""{storedProcedureName}""()"
            : $@"CALL ""{storedProcedureName}""({string.Join(", ", args)})";
    }

    private static PropertyInfo[] GetInputProperties(object parameter) =>
        parameter.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToArray();

    private static T? FromJson<T>(object? value)
    {
        if (value is null)
        {
            return default;
        }

        return value switch
        {
            string s when string.IsNullOrWhiteSpace(s) || s == "null" => default,
            string s => JsonSerializer.Deserialize<T>(s, JsonOptions),
            JsonDocument doc => doc.RootElement.Deserialize<T>(JsonOptions),
            JsonElement el => el.Deserialize<T>(JsonOptions),
            T typed => typed,
            _ => JsonSerializer.Deserialize<T>(value.ToString()!, JsonOptions)
        };
    }

    private async Task<T> RunAsync<T>(string sql, Func<IDbConnection, Task<T>> action)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return await action(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stored procedure command failed.");
            throw;
        }
    }
}
