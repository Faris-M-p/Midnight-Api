namespace MidnightApi.Data;

public static class StoredProcedures
{
    public const string FamilySelect = @"SELECT * FROM ""ProFamilySelect""(@p_id)";
    public const string FamilyInsert = @"SELECT * FROM ""ProFamilyInsert""(@p_family_code, @p_family_name, @p_description, @p_created_by)";
    public const string FamilyUpdate = @"SELECT * FROM ""ProFamilyUpdate""(@p_id, @p_family_name, @p_description, @p_updated_by)";
    public const string FamilyExistsByCode = @"SELECT ""ProFamilyExistsByCode""(@p_code, @p_exclude_family_id)";

    public const string AccountSelect = @"SELECT * FROM ""ProAccountSelect""(@p_id)";
    public const string AccountLogin = @"SELECT * FROM ""ProAccountLogin""(@p_username)";
    public const string AccountRegister = @"SELECT * FROM ""ProAccountRegister""(@p_fk_families, @p_username, @p_email, @p_password_hash, @p_is_active, @p_created_by)";
    public const string AccountUpdate = @"SELECT * FROM ""ProAccountUpdate""(@p_id, @p_username, @p_email, @p_password_hash, @p_updated_by)";
    public const string AccountExistsByUsername = @"SELECT ""ProAccountExistsByUsername""(@p_username, @p_exclude_id)";

    public const string MemberList = @"SELECT * FROM ""ProMemberList""(@p_family_id, @p_search, @p_gender, @p_sort_by, @p_sort_desc, @p_page, @p_page_size)";
    public const string MemberTree = @"SELECT * FROM ""ProMemberTree""(@p_family_id)";
    public const string MemberSelect = @"SELECT * FROM ""ProMemberSelect""(@p_family_id, @p_member_id)";
    public const string MemberInsert = @"SELECT ""ProMemberInsert""(@p_family_id, @p_parent_id, @p_spouse_id, @p_first_name, @p_last_name, @p_email, @p_phone, @p_gender, @p_dob, @p_dod, @p_is_root, @p_nickname, @p_biography, @p_profession, @p_addresses::jsonb, @p_images::jsonb, @p_events::jsonb, @p_notes::jsonb, @p_social_links::jsonb, @p_created_by)";
    public const string MemberUpdate = @"SELECT ""ProMemberUpdate""(@p_family_id, @p_member_id, @p_parent_id, @p_spouse_id, @p_first_name, @p_last_name, @p_email, @p_phone, @p_gender, @p_dob, @p_dod, @p_is_root, @p_nickname, @p_biography, @p_profession, @p_addresses::jsonb, @p_images::jsonb, @p_events::jsonb, @p_notes::jsonb, @p_social_links::jsonb, @p_updated_by)";
    public const string MemberDelete = @"SELECT ""ProMemberDelete""(@p_family_id, @p_member_id, @p_deleted_by)";
    public const string MemberMapSpouse = @"SELECT ""ProMemberMapSpouse""(@p_family_id, @p_member_id, @p_spouse_id, @p_updated_by)";
    public const string MemberDashboard = @"SELECT * FROM ""ProMemberDashboard""(@p_family_id)";
    public const string MemberTimeline = @"SELECT * FROM ""ProMemberTimeline""(@p_family_id)";
    public const string MemberExistsInFamily = @"SELECT ""ProMemberExistsInFamily""(@p_family_id, @p_member_id)";
    public const string MemberHasRoot = @"SELECT ""ProMemberHasRoot""(@p_family_id, @p_exclude_member_id)";
    public const string MemberGetParentId = @"SELECT ""ProMemberGetParentId""(@p_member_id)";
    public const string MemberGetRelation = @"SELECT * FROM ""ProMemberGetRelation""(@p_family_id, @p_member_id)";
}
