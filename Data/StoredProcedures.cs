namespace MidnightApi.Data;

public static class StoredProcedures
{
    public const string FamilySelect = "ProFamilySelect";
    public const string FamilyInsert = "ProFamilyInsert";
    public const string FamilyUpdate = "ProFamilyUpdate";
    public const string FamilyUpdateCover = "ProFamilyUpdateCover";
    public const string FamilyExistsByCode = "ProFamilyExistsByCode";
    public const string FamilySelectByCode = "ProFamilySelectByCode";

    public const string AccountSelect = "ProAccountSelect";
    public const string AccountLogin = "ProAccountLogin";
    public const string AccountRegister = "ProAccountRegister";
    public const string AccountUpdate = "ProAccountUpdate";
    public const string AccountExistsByUsername = "ProAccountExistsByUsername";
    public const string AccountSelectByEmail = "ProAccountSelectByEmail";
    public const string AccountSetEmailVerified = "ProAccountSetEmailVerified";
    public const string AccountUpdatePassword = "ProAccountUpdatePassword";
    public const string AccountOtpInsert = "ProAccountOtpInsert";
    public const string AccountOtpInvalidate = "ProAccountOtpInvalidate";
    public const string AccountOtpSelectActive = "ProAccountOtpSelectActive";
    public const string AccountOtpSelectById = "ProAccountOtpSelectById";
    public const string AccountOtpIncrementAttempt = "ProAccountOtpIncrementAttempt";
    public const string AccountOtpMarkUsed = "ProAccountOtpMarkUsed";
    public const string AccountOtpSelectByResetToken = "ProAccountOtpSelectByResetToken";
    public const string AccountOtpClearResetToken = "ProAccountOtpClearResetToken";

    public const string MemberList = "ProMemberList";
    public const string MemberTree = "ProMemberTree";
    public const string MemberSelect = "ProMemberSelect";
    public const string MemberInsert = "ProMemberInsert";
    public const string MemberUpdate = "ProMemberUpdate";
    public const string MemberDelete = "ProMemberDelete";
    public const string MemberMapSpouse = "ProMemberMapSpouse";
    public const string MemberDashboard = "ProMemberDashboard";
    public const string MemberTimeline = "ProMemberTimeline";
    public const string MemberIsInScopeBranch = "ProMemberIsInScopeBranch";

    public const string AccessTokenList = "ProAccessTokenList";
    public const string AccessTokenSelect = "ProAccessTokenSelect";
    public const string AccessTokenInsert = "ProAccessTokenInsert";
    public const string AccessTokenUpdate = "ProAccessTokenUpdate";
    public const string AccessTokenSetStatus = "ProAccessTokenSetStatus";
    public const string AccessTokenDelete = "ProAccessTokenDelete";
    public const string AccessTokenListForLogin = "ProAccessTokenListForLogin";
    public const string AccessTokenRecordLogin = "ProAccessTokenRecordLogin";
}
