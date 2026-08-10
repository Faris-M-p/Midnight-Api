using System.Security.Cryptography;

namespace MidnightApi.Services;

public static class AccessTokenSecretGenerator
{
    private static readonly char[] Alphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray();

    /// <summary>
    /// Builds {FamilyCode}_{SecureRandom} without exposing the numeric FamilyId.
    /// Non-alphanumeric characters are stripped from the family code for a stable token prefix.
    /// </summary>
    public static string GenerateRawToken(string? familyCode, int secretLength = 20)
    {
        var prefix = PrefixFromFamilyCode(familyCode);
        var secret = GenerateSecret(secretLength);
        return $"{prefix}_{secret}";
    }

    public static string PrefixFromFamilyCode(string? familyCode)
    {
        var cleaned = new string((familyCode ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToUpperInvariant();

        return string.IsNullOrWhiteSpace(cleaned) ? "FAMILY" : cleaned;
    }

    public static bool MatchesFamilyPrefix(string rawToken, string? familyCode)
    {
        var prefix = PrefixFromFamilyCode(familyCode);
        return rawToken.StartsWith(prefix + "_", StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses {FamilyCodePrefix}_{SecureSecret}. Uses the first underscore as the separator
    /// so secrets may themselves contain underscores if ever present.
    /// </summary>
    public static bool TryParse(string? rawToken, out string familyCodePrefix, out string secret)
    {
        familyCodePrefix = string.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return false;
        }

        var value = rawToken.Trim();
        var separator = value.IndexOf('_');
        if (separator <= 0 || separator >= value.Length - 1)
        {
            return false;
        }

        familyCodePrefix = value[..separator];
        secret = value[(separator + 1)..];
        return familyCodePrefix.Length > 0 && secret.Length > 0;
    }

    public static string BuildPreview(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return "••••••••";
        }

        var parts = rawToken.Split('_', 2);
        if (parts.Length != 2 || parts[1].Length < 4)
        {
            var safe = rawToken.Length <= 8 ? rawToken : $"{rawToken[..4]}…{rawToken[^2..]}";
            return safe;
        }

        var tokenPrefix = parts[0];
        var secret = parts[1];
        var prefixPreview = tokenPrefix.Length <= 6 ? tokenPrefix : tokenPrefix[..6];
        return $"{prefixPreview}…{secret[^4..]}";
    }

    private static string GenerateSecret(int length)
    {
        var lengthClamped = Math.Clamp(length, 16, 48);
        Span<char> buffer = stackalloc char[lengthClamped];
        Span<byte> bytes = stackalloc byte[lengthClamped];
        RandomNumberGenerator.Fill(bytes);
        for (var i = 0; i < lengthClamped; i++)
        {
            buffer[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(buffer);
    }
}

public static class AccessTokenExpiryHelper
{
    public static DateTimeOffset ResolveExpiresOn(string expiryPreset, DateTimeOffset? customExpiresOn)
    {
        var now = DateTimeOffset.UtcNow;
        return expiryPreset.Trim() switch
        {
            "30Days" => now.AddDays(30),
            "90Days" => now.AddDays(90),
            "6Months" => now.AddMonths(6),
            "1Year" => now.AddYears(1),
            "Custom" => customExpiresOn
                ?? throw new Exceptions.BadRequestException("Custom expiry date is required."),
            _ => throw new Exceptions.BadRequestException("Invalid expiry option.")
        };
    }

    public static string InferPreset(DateTimeOffset expiresOn)
    {
        var days = (expiresOn - DateTimeOffset.UtcNow).TotalDays;
        if (Math.Abs(days - 30) < 1.5) return "30Days";
        if (Math.Abs(days - 90) < 1.5) return "90Days";
        if (Math.Abs(days - 182) < 3) return "6Months";
        if (Math.Abs(days - 365) < 3) return "1Year";
        return "Custom";
    }
}
