namespace MidnightApi.Services;

using MidnightApi.Services.Interfaces;

public class FamilyCodeGenerator : IFamilyCodeGenerator
{
    private static readonly char[] Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public string Generate(string? familyName)
    {
        return $"{DerivePrefix(familyName)}-{RandomSuffix(4)}";
    }

    private static string DerivePrefix(string? familyName)
    {
        var firstWord = (familyName ?? string.Empty)
            .Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        var cleaned = new string(firstWord.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (cleaned.Length < 2)
        {
            return "FAM";
        }

        return cleaned.Length > 12 ? cleaned[..12] : cleaned;
    }

    private static string RandomSuffix(int length)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
