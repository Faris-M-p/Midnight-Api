using System.Security.Claims;

namespace MidnightApi.Services.Interfaces;

public interface IAccessAuthorizationService
{
    void EnsureFamilyMatches(ClaimsPrincipal user, long familyId);
    void EnsureCanManageAccessTokens(ClaimsPrincipal user);
    void EnsureCanEditFamily(ClaimsPrincipal user);
    Task EnsureCanEditMemberAsync(ClaimsPrincipal user, long memberId);
    Task EnsureCanDeleteMemberAsync(ClaimsPrincipal user, long memberId);
    Task EnsureCanCreateMemberAsync(ClaimsPrincipal user, long? parentId);
    Task EnsureCanMapSpouseAsync(ClaimsPrincipal user, long memberId, long spouseId);
}
