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
        await RemoveLegacyShadowForeignKeysAsync(db);
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

        var escapedDbName = targetDatabase.Replace("\"", "\"\"");
        await using var createCommand =
            new NpgsqlCommand($"CREATE DATABASE \"{escapedDbName}\"", connection);
        await createCommand.ExecuteNonQueryAsync();
    }

    private static async Task RemoveLegacyShadowForeignKeysAsync(DbConnectionClass db)
    {
        // Cleanup old EF-convention shadow FK columns left from previous schema versions.
        // Current model uses only FK_Members for child tables.
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "MemberAddresses" DROP CONSTRAINT IF EXISTS "FK_MemberAddresses_Members_MemberID_Members";
            ALTER TABLE "MemberAddresses" DROP COLUMN IF EXISTS "MemberID_Members";

            ALTER TABLE "MemberImages" DROP CONSTRAINT IF EXISTS "FK_MemberImages_Members_MemberID_Members";
            ALTER TABLE "MemberImages" DROP COLUMN IF EXISTS "MemberID_Members";

            ALTER TABLE "MemberEvents" DROP CONSTRAINT IF EXISTS "FK_MemberEvents_Members_MemberID_Members";
            ALTER TABLE "MemberEvents" DROP COLUMN IF EXISTS "MemberID_Members";

            ALTER TABLE "MemberNotes" DROP CONSTRAINT IF EXISTS "FK_MemberNotes_Members_MemberID_Members";
            ALTER TABLE "MemberNotes" DROP COLUMN IF EXISTS "MemberID_Members";

            ALTER TABLE "MemberSocialLinks" DROP CONSTRAINT IF EXISTS "FK_MemberSocialLinks_Members_MemberID_Members";
            ALTER TABLE "MemberSocialLinks" DROP COLUMN IF EXISTS "MemberID_Members";
            """);
    }
}
