namespace MidnightApi.Services.Interfaces;

public interface IAccessTokenSecretGenerator
{
    string GenerateRawToken(string? familyCode, int secretLength = 20);
    string PrefixFromFamilyCode(string? familyCode);
    bool MatchesFamilyPrefix(string rawToken, string? familyCode);
    bool TryParse(string? rawToken, out string familyCodePrefix, out string secret);
    string BuildPreview(string rawToken);
    DateTimeOffset ResolveExpiresOn(string expiryPreset, DateTimeOffset? customExpiresOn);
    string InferPreset(DateTimeOffset expiresOn);
}
