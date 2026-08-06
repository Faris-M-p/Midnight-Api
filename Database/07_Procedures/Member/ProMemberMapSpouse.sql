CREATE OR REPLACE FUNCTION "ProMemberMapSpouse"(
    p_family_id  BIGINT,
    p_member_id  BIGINT,
    p_spouse_id  BIGINT,
    p_updated_by VARCHAR
)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "Members"
    SET "FK_Members_Spouse" = p_spouse_id,
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE "ID_Members" = p_member_id
      AND "FK_Families" = p_family_id
      AND "IsCancelled" = FALSE;

    UPDATE "Members"
    SET "FK_Members_Spouse" = p_member_id,
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE "ID_Members" = p_spouse_id
      AND "FK_Families" = p_family_id
      AND "IsCancelled" = FALSE;
END;
$$;
