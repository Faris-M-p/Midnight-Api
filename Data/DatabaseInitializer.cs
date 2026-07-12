using Microsoft.EntityFrameworkCore;
using MidnightApi.Models.Entities;
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
        await SeedDemoDataAsync(db);
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

    private static async Task SeedDemoDataAsync(DbConnectionClass db)
    {
        if (await db.Families.AnyAsync())
        {
            return;
        }

        var family = new Family
        {
            FamilyCode = "DEMO-001",
            FamilyName = "Demo Family",
            Description = "Demo family for Midnight Family Tree",
            CreatedBy = "seed"
        };
        db.Families.Add(family);
        await db.SaveChangesAsync();

        var root = new Member
        {
            FK_Families = family.ID_Families,
            FirstName = "John",
            LastName = "Demo",
            Email = "john.demo@example.com",
            Gender = "Male",
            DateOfBirth = new DateOnly(1950, 1, 15),
            IsRoot = true,
            CreatedBy = "seed"
        };
        var spouse = new Member
        {
            FK_Families = family.ID_Families,
            FirstName = "Jane",
            LastName = "Demo",
            Email = "jane.demo@example.com",
            Gender = "Female",
            DateOfBirth = new DateOnly(1952, 3, 20),
            CreatedBy = "seed"
        };
        db.Members.AddRange(root, spouse);
        await db.SaveChangesAsync();

        root.FK_Members_Spouse = spouse.ID_Members;
        spouse.FK_Members_Spouse = root.ID_Members;
        await db.SaveChangesAsync();

        var child1 = new Member { FK_Families = family.ID_Families, FK_Members_Parent = root.ID_Members, FirstName = "Michael", LastName = "Demo", Gender = "Male", DateOfBirth = new DateOnly(1975, 6, 10), CreatedBy = "seed" };
        var child2 = new Member { FK_Families = family.ID_Families, FK_Members_Parent = root.ID_Members, FirstName = "Sarah", LastName = "Demo", Gender = "Female", DateOfBirth = new DateOnly(1978, 9, 5), CreatedBy = "seed" };
        db.Members.AddRange(child1, child2);
        await db.SaveChangesAsync();

        db.Members.AddRange(
            new Member { FK_Families = family.ID_Families, FK_Members_Parent = child1.ID_Members, FirstName = "Emily", LastName = "Demo", Gender = "Female", DateOfBirth = new DateOnly(2000, 2, 14), CreatedBy = "seed" },
            new Member { FK_Families = family.ID_Families, FK_Members_Parent = child2.ID_Members, FirstName = "David", LastName = "Demo", Gender = "Male", DateOfBirth = new DateOnly(2003, 11, 30), CreatedBy = "seed" });
        await db.SaveChangesAsync();
    }
}
