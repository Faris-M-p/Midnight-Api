using System.Security.Claims;

namespace MidnightApi.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long GetAccountId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.AccountId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(value, out var accountId) || accountId <= 0)
        {
            throw new UnauthorizedAccessException("Account id claim is missing.");
        }

        return accountId;
    }

    public static bool TryGetAccountId(this ClaimsPrincipal user, out long accountId)
    {
        var value = user.FindFirstValue(AuthClaimTypes.AccountId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out accountId) && accountId > 0;
    }

    public static long GetFamilyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.FamilyId);
        if (!long.TryParse(value, out var familyId))
        {
            throw new UnauthorizedAccessException("Family id claim is missing.");
        }

        return familyId;
    }

    public static string GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Name)
            ?? user.Identity?.Name
            ?? throw new UnauthorizedAccessException("Username claim is missing.");
    }

    public static string GetAuthType(this ClaimsPrincipal user) =>
        user.FindFirstValue(AuthClaimTypes.AuthType) ?? AuthTypes.Admin;

    public static bool IsAdminUser(this ClaimsPrincipal user) =>
        user.IsInRole(AuthRoles.Admin)
        || string.Equals(user.FindFirstValue(AuthClaimTypes.Permission), AuthPermissions.AdminFull, StringComparison.OrdinalIgnoreCase);

    public static bool IsAccessTokenUser(this ClaimsPrincipal user) =>
        string.Equals(user.GetAuthType(), AuthTypes.AccessToken, StringComparison.OrdinalIgnoreCase)
        || user.IsInRole(AuthRoles.TokenUser);

    public static string GetAccessPermission(this ClaimsPrincipal user)
    {
        if (user.IsAdminUser())
        {
            return AuthPermissions.AdminFull;
        }

        return user.FindFirstValue(AuthClaimTypes.Permission) ?? AuthPermissions.View;
    }

    public static bool CanEdit(this ClaimsPrincipal user) =>
        user.IsAdminUser()
        || string.Equals(user.GetAccessPermission(), AuthPermissions.Edit, StringComparison.OrdinalIgnoreCase);

    public static string? GetScopeType(this ClaimsPrincipal user) =>
        user.FindFirstValue(AuthClaimTypes.Scope);

    public static long? GetScopeMemberId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.ScopeMemberId);
        if (long.TryParse(value, out var memberId) && memberId > 0)
        {
            return memberId;
        }

        return null;
    }

    public static long? GetAccessTokenId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.AccessTokenId);
        if (long.TryParse(value, out var tokenId) && tokenId > 0)
        {
            return tokenId;
        }

        return null;
    }
}
