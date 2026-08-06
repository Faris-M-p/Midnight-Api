using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace MidnightApi.DataAccess;

public sealed class DataAccessDapper : IDataAccessDapper
{
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
        var sql = BuildSelectSql(storedProcedureName, parameter);
        var rows = await RunAsync(sql, conn => conn.QueryAsync<TResult>(sql, parameter));
        return rows.AsList();
    }

    public Task<TResult?> GetSingleOrDefaultByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var sql = BuildSelectSql(storedProcedureName, parameter);
        return RunAsync(sql, conn => conn.QuerySingleOrDefaultAsync<TResult>(sql, parameter));
    }

    public Task<TResult> GetSingleByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var sql = BuildSelectSql(storedProcedureName, parameter);
        return RunAsync(sql, conn => conn.QuerySingleAsync<TResult>(sql, parameter));
    }

    public async Task<TResult?> GetPayloadByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var sql = BuildSelectSql(storedProcedureName, parameter);
        var payload = await RunAsync(sql, conn => conn.ExecuteScalarAsync<object>(sql, parameter));
        return FromJson<TResult>(payload);
    }

    public Task<TResult?> ExecuteScalarByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null)
    {
        var sql = BuildScalarSql(storedProcedureName, parameter);
        return RunAsync(sql, conn => conn.ExecuteScalarAsync<TResult>(sql, parameter));
    }

    public Task<int> ExecuteByStoredProcedureAsync(string storedProcedureName, object? parameter = null)
    {
        var sql = BuildScalarSql(storedProcedureName, parameter);
        return RunAsync(sql, conn => conn.ExecuteAsync(sql, parameter));
    }

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
            _ => JsonSerializer.Deserialize<T>(value.ToString()!, JsonOptions)
        };
    }

    private static string BuildSelectSql(string storedProcedureName, object? parameter)
    {
        var args = BuildNamedArguments(parameter);
        return string.IsNullOrEmpty(args)
            ? $@"SELECT * FROM ""{storedProcedureName}""()"
            : $@"SELECT * FROM ""{storedProcedureName}""({args})";
    }

    private static string BuildScalarSql(string storedProcedureName, object? parameter)
    {
        var args = BuildNamedArguments(parameter);
        return string.IsNullOrEmpty(args)
            ? $@"SELECT ""{storedProcedureName}""()"
            : $@"SELECT ""{storedProcedureName}""({args})";
    }

    private static string BuildNamedArguments(object? parameter)
    {
        if (parameter is null)
        {
            return string.Empty;
        }

        var properties = parameter.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .ToArray();

        return string.Join(", ", properties.Select(p =>
        {
            var dbName = p.GetCustomAttribute<DbParamAttribute>()?.Name ?? p.Name;
            return $"{dbName} := @{p.Name}";
        }));
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
