CREATE OR REPLACE FUNCTION "ProMemberMapSpouse"(
    p_family_id  BIGINT,
    p_member_id  BIGINT,
    p_spouse_id  BIGINT,
    p_updated_by VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_code INTEGER;
    v_status INTEGER;
    v_message VARCHAR;
BEGIN
    SELECT v."ResponseCode", v."StatusCode", v."ResponseMessage"
    INTO v_code, v_status, v_message
    FROM "FnMemberValidateMapSpouse"(p_family_id, p_member_id, p_spouse_id) v;

    IF v_code <> 0 THEN
        RETURN QUERY SELECT v_code, v_status, v_message;
        RETURN;
    END IF;

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

    RETURN QUERY SELECT 0, 200, 'Spouse relationship mapped successfully.'::VARCHAR;
END;
$$;
