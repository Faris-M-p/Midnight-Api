CREATE OR REPLACE FUNCTION "ProMemberDelete"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    p_deleted_by VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_spouse_id BIGINT;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = p_member_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 1, 404, 'Member not found.'::VARCHAR;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1 FROM "Members"
        WHERE "FK_Members_Parent" = p_member_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 2, 400, 'Cannot delete this member because they have children. Remove or reassign children first.'::VARCHAR;
        RETURN;
    END IF;

    SELECT "FK_Members_Spouse" INTO v_spouse_id
    FROM "Members"
    WHERE "ID_Members" = p_member_id;

    IF v_spouse_id IS NOT NULL THEN
        UPDATE "Members"
        SET "FK_Members_Spouse" = NULL,
            "UpdatedBy" = p_deleted_by,
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = v_spouse_id
          AND "IsCancelled" = FALSE;
    END IF;

    UPDATE "Members"
    SET "FK_Members_Spouse" = NULL,
        "IsCancelled" = TRUE,
        "CancelledBy" = p_deleted_by,
        "CancelledOn" = NOW()
    WHERE "ID_Members" = p_member_id;

    RETURN QUERY SELECT 0, 200, 'Member deleted successfully.'::VARCHAR;
END;
$$;
