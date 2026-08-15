using System.Security.Claims;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Services;

/// <summary>
/// Enforces edit authorization for access-token sessions.
/// Read access is always family-wide for valid tokens; scope applies only to writes.
/// </summary>
public class AccessAuthorizationService : IAccessAuthorizationService
{
    private readonly IMembersRepository _members;

    public AccessAuthorizationService(IMembersRepository members)
    {
        _members = members;
    }

    public void EnsureFamilyMatches(ClaimsPrincipal user, long familyId)
    {
        if (user.GetFamilyId() != familyId)
        {
            throw new ForbiddenException();
        }
    }

    public void EnsureCanManageAccessTokens(ClaimsPrincipal user)
    {
        if (!user.IsAdminUser())
        {
            throw new ForbiddenException();
        }
    }

    public void EnsureCanEditFamily(ClaimsPrincipal user)
    {
        // Family settings remain admin-only until a clear family-edit scope rule exists.
        if (!user.IsAdminUser())
        {
            throw new ForbiddenException();
        }
    }

    public async Task EnsureCanEditMemberAsync(ClaimsPrincipal user, long memberId)
    {
        await EnsureMemberWriteAllowedAsync(user, memberId);
    }

    public async Task EnsureCanDeleteMemberAsync(ClaimsPrincipal user, long memberId)
    {
        await EnsureMemberWriteAllowedAsync(user, memberId);
    }

    public async Task EnsureCanCreateMemberAsync(ClaimsPrincipal user, long? parentId)
    {
        if (user.IsAdminUser())
        {
            return;
        }

        EnsureHasEditPermission(user);

        var scope = user.GetScopeType();
        if (string.Equals(scope, AccessTokenScopes.EntireFamily, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(scope, AccessTokenScopes.SelectedMember, StringComparison.OrdinalIgnoreCase))
        {
            // Selected-member scope may edit only that member — not create new members.
            throw new ForbiddenException();
        }

        if (string.Equals(scope, AccessTokenScopes.MemberDescendants, StringComparison.OrdinalIgnoreCase))
        {
            var rootId = user.GetScopeMemberId()
                ?? throw new ForbiddenException();
            if (parentId is null or <= 0)
            {
                throw new ForbiddenException();
            }

            var allowed = await IsInScopeBranchAsync(user.GetFamilyId(), rootId, parentId.Value);
            if (!allowed)
            {
                throw new ForbiddenException();
            }

            return;
        }

        throw new ForbiddenException();
    }

    public async Task EnsureCanMapSpouseAsync(ClaimsPrincipal user, long memberId, long spouseId)
    {
        if (user.IsAdminUser())
        {
            return;
        }

        EnsureHasEditPermission(user);

        var scope = user.GetScopeType();
        if (string.Equals(scope, AccessTokenScopes.EntireFamily, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(scope, AccessTokenScopes.SelectedMember, StringComparison.OrdinalIgnoreCase))
        {
            var rootId = user.GetScopeMemberId()
                ?? throw new ForbiddenException();
            // Relationship edit is allowed only when the scoped member is the subject.
            if (memberId != rootId)
            {
                throw new ForbiddenException();
            }

            return;
        }

        if (string.Equals(scope, AccessTokenScopes.MemberDescendants, StringComparison.OrdinalIgnoreCase))
        {
            var rootId = user.GetScopeMemberId()
                ?? throw new ForbiddenException();
            var familyId = user.GetFamilyId();
            var memberOk = await IsInScopeBranchAsync(familyId, rootId, memberId);
            var spouseOk = await IsInScopeBranchAsync(familyId, rootId, spouseId);
            if (!memberOk || !spouseOk)
            {
                throw new ForbiddenException();
            }

            return;
        }

        throw new ForbiddenException();
    }

    private async Task EnsureMemberWriteAllowedAsync(ClaimsPrincipal user, long memberId)
    {
        if (user.IsAdminUser())
        {
            return;
        }

        EnsureHasEditPermission(user);

        var scope = user.GetScopeType();
        if (string.Equals(scope, AccessTokenScopes.EntireFamily, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var rootId = user.GetScopeMemberId()
            ?? throw new ForbiddenException();

        if (string.Equals(scope, AccessTokenScopes.SelectedMember, StringComparison.OrdinalIgnoreCase))
        {
            if (memberId != rootId)
            {
                throw new ForbiddenException();
            }

            return;
        }

        if (string.Equals(scope, AccessTokenScopes.MemberDescendants, StringComparison.OrdinalIgnoreCase))
        {
            var allowed = await IsInScopeBranchAsync(user.GetFamilyId(), rootId, memberId);
            if (!allowed)
            {
                throw new ForbiddenException();
            }

            return;
        }

        throw new ForbiddenException();
    }

    private static void EnsureHasEditPermission(ClaimsPrincipal user)
    {
        if (!user.CanEdit())
        {
            throw new ForbiddenException();
        }
    }

    private Task<bool> IsInScopeBranchAsync(long familyId, long rootMemberId, long candidateMemberId) =>
        _members.IsInScopeBranchAsync(new InputMemberScopeCheck
        {
            FamilyId = familyId,
            RootMemberId = rootMemberId,
            CandidateMemberId = candidateMemberId
        });
}
