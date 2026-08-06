using Npgsql;

namespace MidnightApi.Data;

public static class DatabaseInitializer
{
    public static async Task EnsureDatabaseAsync(string connectionString)
    {
        await EnsureDatabaseExistsAsync(connectionString);

        var databaseRoot = ResolveDatabaseRoot();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await connection.EnsureSchemaAsync(databaseRoot);
    }

    private static string ResolveDatabaseRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Database"),
            Path.Combine(Directory.GetCurrentDirectory(), "Database"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Database")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Database"))
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Database.sql")))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException(
            "Database project folder was not found. Expected a Database directory with Database.sql.");
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

        if (await existsCommand.ExecuteScalarAsync() is not null)
        {
            return;
        }

        var escapedDbName = targetDatabase.Replace("\"", "\"\"", StringComparison.Ordinal);
        await using var createCommand =
            new NpgsqlCommand($"CREATE DATABASE \"{escapedDbName}\"", connection);
        await createCommand.ExecuteNonQueryAsync();
    }
}
