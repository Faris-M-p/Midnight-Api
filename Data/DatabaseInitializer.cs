using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MidnightApi.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureCreatedAsync(IServiceProvider services, string connectionString)
    {
        await EnsureDatabaseExistsAsync(connectionString);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbConnectionClass>();
        await db.Database.EnsureCreatedAsync();
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.Database;

        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            throw new InvalidOperationException("Database name is missing in connection string.");
        }

        var maintenanceBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(maintenanceBuilder.ConnectionString);
        await connection.OpenAsync();

        await using var existsCommand =
            new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @dbName", connection);
        existsCommand.Parameters.AddWithValue("dbName", targetDatabase);

        var exists = await existsCommand.ExecuteScalarAsync();
        if (exists is not null)
        {
            return;
        }

        var escapedDbName = targetDatabase.Replace("\"", "\"\"");
        await using var createCommand =
            new NpgsqlCommand($"CREATE DATABASE \"{escapedDbName}\"", connection);
        await createCommand.ExecuteNonQueryAsync();
    }
}
