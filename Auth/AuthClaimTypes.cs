namespace MidnightApi.Auth;

public static class AuthClaimTypes
{
    public const string AccountId = "account_id";
    public const string FamilyId = "family_id";
    public const string Permission = "permission";
    public const string AuthType = "auth_type";
    public const string AccessTokenId = "access_token_id";
    public const string Scope = "scope";
    public const string ScopeMemberId = "scope_member_id";
}

public static class AuthRoles
{
    public const string Admin = "Admin";
    public const string TokenUser = "TokenUser";
}

public static class AuthPermissions
{
    public const string AdminFull = "ADMIN_FULL";
    public const string View = "View";
    public const string Edit = "Edit";
}

public static class AuthTypes
{
    public const string Admin = "admin";
    public const string AccessToken = "access_token";
}

public static class AccessTokenScopes
{
    public const string EntireFamily = "EntireFamily";
    public const string SelectedMember = "SelectedMember";
    public const string MemberDescendants = "MemberDescendants";
}
