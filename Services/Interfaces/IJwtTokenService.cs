namespace MidnightApi.Services.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAdminToken(long accountId, long familyId, string username);

    (string Token, DateTime ExpiresAtUtc) CreateAccessTokenSession(
        long familyId,
        long accessTokenId,
        string displayName,
        string permission,
        string scope,
        long? scopeMemberId,
        DateTimeOffset accessTokenExpiresOn);
}
