using System.Text.RegularExpressions;

namespace MidnightApi.Services;

public partial class MemberValidationService
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[0-9\s\-().]{7,20}$")]
    private static partial Regex PhoneRegex();

    public string? ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || EmailRegex().IsMatch(email))
        {
            return null;
        }

        return "Invalid email format.";
    }

    public string? ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || PhoneRegex().IsMatch(phone))
        {
            return null;
        }

        return "Invalid phone format.";
    }

    public string? ValidateSelfReference(long memberId, long? parentId, long? spouseId)
    {
        if (parentId.HasValue && parentId.Value == memberId)
        {
            return "A member cannot be their own parent.";
        }

        if (spouseId.HasValue && spouseId.Value == memberId)
        {
            return "A member cannot marry themselves.";
        }

        return null;
    }

    public string? ValidateRootMember(bool isRoot, bool existingRootInFamily)
    {
        if (isRoot && existingRootInFamily)
        {
            return "Only one root member is allowed per family.";
        }

        return null;
    }

    public async Task<string?> ValidateCircularParentAsync(
        long memberId,
        long? proposedParentId,
        Func<long, Task<long?>> getParentIdAsync)
    {
        if (!proposedParentId.HasValue)
        {
            return null;
        }

        if (proposedParentId.Value == memberId)
        {
            return "A member cannot be their own parent.";
        }

        var visited = new HashSet<long>();
        if (memberId > 0)
        {
            visited.Add(memberId);
        }

        var current = proposedParentId.Value;

        while (true)
        {
            if (!visited.Add(current))
            {
                return "Circular parent reference detected.";
            }

            var next = await getParentIdAsync(current);
            if (!next.HasValue)
            {
                break;
            }

            current = next.Value;
        }

        return null;
    }

    public async Task<string?> ValidateMapSpouseAsync(
        long memberId,
        long spouseId,
        Func<long, Task<(long? ParentId, long? SpouseId)>> getRelationAsync)
    {
        if (memberId == spouseId)
        {
            return "A member cannot marry themselves.";
        }

        var left = await getRelationAsync(memberId);
        var right = await getRelationAsync(spouseId);

        if (left.SpouseId.HasValue || right.SpouseId.HasValue)
        {
            return "One or both members already have a spouse.";
        }

        if (left.ParentId == spouseId || right.ParentId == memberId)
        {
            return "Parent-child relationship cannot be mapped as spouses.";
        }

        if (left.ParentId.HasValue && right.ParentId.HasValue && left.ParentId == right.ParentId)
        {
            return "Sibling relationship cannot be mapped as spouses.";
        }

        if (await IsAncestorAsync(memberId, spouseId, async id => (await getRelationAsync(id)).ParentId)
            || await IsAncestorAsync(spouseId, memberId, async id => (await getRelationAsync(id)).ParentId))
        {
            return "Ancestor-descendant relationship cannot be mapped as spouses.";
        }

        return null;
    }

    private static async Task<bool> IsAncestorAsync(
        long ancestorId,
        long descendantId,
        Func<long, Task<long?>> getParentIdAsync)
    {
        var current = await getParentIdAsync(descendantId);
        var visited = new HashSet<long>();

        while (current.HasValue && visited.Add(current.Value))
        {
            if (current.Value == ancestorId)
            {
                return true;
            }

            current = await getParentIdAsync(current.Value);
        }

        return false;
    }
}
