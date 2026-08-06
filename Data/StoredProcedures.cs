namespace MidnightApi.Data;

public static class StoredProcedures
{
    public const string FamilySelect = "ProFamilySelect";
    public const string FamilyInsert = "ProFamilyInsert";
    public const string FamilyUpdate = "ProFamilyUpdate";
    public const string FamilyExistsByCode = "ProFamilyExistsByCode";

    public const string AccountSelect = "ProAccountSelect";
    public const string AccountLogin = "ProAccountLogin";
    public const string AccountRegister = "ProAccountRegister";
    public const string AccountUpdate = "ProAccountUpdate";
    public const string AccountExistsByUsername = "ProAccountExistsByUsername";

    public const string MemberList = "ProMemberList";
    public const string MemberTree = "ProMemberTree";
    public const string MemberSelect = "ProMemberSelect";
    public const string MemberInsert = "ProMemberInsert";
    public const string MemberUpdate = "ProMemberUpdate";
    public const string MemberDelete = "ProMemberDelete";
    public const string MemberMapSpouse = "ProMemberMapSpouse";
    public const string MemberDashboard = "ProMemberDashboard";
    public const string MemberTimeline = "ProMemberTimeline";
}
