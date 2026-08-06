using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace MidnightApi.Data;

public static class DapperExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    public static string? ToJsonb(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);

    public static T? FromJsonb<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public static T? FromJsonb<T>(object? value)
    {
        return value switch
        {
            null => default,
            string s => FromJsonb<T>(s),
            JsonDocument doc => doc.RootElement.Deserialize<T>(JsonOptions),
            JsonElement el => el.Deserialize<T>(JsonOptions),
            _ => FromJsonb<T>(value.ToString())
        };
    }

    public static async Task EnsureSchemaAsync(this IDbConnection connection, string databaseRootPath)
    {
        var tablesExist = await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name = 'Families'
            );
            """);

        var tableFiles = new[]
        {
            Path.Combine("01_Tables", "Families.sql"),
            Path.Combine("01_Tables", "Members.sql"),
            Path.Combine("01_Tables", "MemberAddresses.sql"),
            Path.Combine("01_Tables", "MemberImages.sql"),
            Path.Combine("01_Tables", "MemberEvents.sql"),
            Path.Combine("01_Tables", "MemberNotes.sql"),
            Path.Combine("01_Tables", "MemberSocialLinks.sql"),
            Path.Combine("01_Tables", "UserAccounts.sql"),
        };

        var alwaysFiles = new[]
        {
            Path.Combine("02_Functions", "FnGetGeneration.sql"),
            Path.Combine("02_Functions", "FnGetRelationship.sql"),
            Path.Combine("04_Views", "ViewDashboard.sql"),
            Path.Combine("04_Views", "ViewMembers.sql"),
            Path.Combine("05_Indexes", "MemberIndexes.sql"),
            Path.Combine("07_Procedures", "Account", "ProAccountSelect.sql"),
            Path.Combine("07_Procedures", "Account", "ProAccountLogin.sql"),
            Path.Combine("07_Procedures", "Account", "ProAccountRegister.sql"),
            Path.Combine("07_Procedures", "Account", "ProAccountUpdate.sql"),
            Path.Combine("07_Procedures", "Account", "ProAccountExistsByUsername.sql"),
            Path.Combine("07_Procedures", "Family", "ProFamilySelect.sql"),
            Path.Combine("07_Procedures", "Family", "ProFamilyInsert.sql"),
            Path.Combine("07_Procedures", "Family", "ProFamilyUpdate.sql"),
            Path.Combine("07_Procedures", "Family", "ProFamilyExistsByCode.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberList.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberTree.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberSelect.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberInsert.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberUpdate.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberDelete.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberMapSpouse.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberDashboard.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberTimeline.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberExistsInFamily.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberHasRoot.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberGetParentId.sql"),
            Path.Combine("07_Procedures", "Member", "ProMemberGetRelation.sql"),
        };

        if (!tablesExist)
        {
            await ExecuteScriptsAsync(connection, databaseRootPath, tableFiles);
        }

        await ExecuteScriptsAsync(connection, databaseRootPath, alwaysFiles);
    }

    private static async Task ExecuteScriptsAsync(IDbConnection connection, string databaseRootPath, IEnumerable<string> relativePaths)
    {
        foreach (var relative in relativePaths)
        {
            var fullPath = Path.Combine(databaseRootPath, relative);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Database script not found: {fullPath}");
            }

            var sql = await File.ReadAllTextAsync(fullPath);
            var meaningful = sql.Split('\n')
                .Select(l => l.Trim())
                .Any(l => l.Length > 0 && !l.StartsWith("--", StringComparison.Ordinal));

            if (!meaningful)
            {
                continue;
            }

            await connection.ExecuteAsync(sql);
        }
    }

    public static bool IsUniqueViolation(this PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.UniqueViolation;
}
