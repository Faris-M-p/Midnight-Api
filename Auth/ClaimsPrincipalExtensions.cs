using System.Security.Claims;

namespace MidnightApi.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long GetAccountId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.AccountId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(value, out var accountId))
        {
            throw new UnauthorizedAccessException("Account id claim is missing.");
        }

        return accountId;
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
}
