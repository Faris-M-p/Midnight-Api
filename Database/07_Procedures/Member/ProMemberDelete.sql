CREATE OR REPLACE FUNCTION "ProMemberDelete"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    p_deleted_by VARCHAR
)
RETURNS INTEGER
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
        RETURN 0; -- not found
    END IF;

    IF EXISTS (
        SELECT 1 FROM "Members"
        WHERE "FK_Members_Parent" = p_member_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN -1; -- has children
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

    RETURN 1; -- success
END;
$$;
