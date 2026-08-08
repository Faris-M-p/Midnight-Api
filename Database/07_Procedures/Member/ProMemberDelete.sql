CREATE OR REPLACE PROCEDURE "ProMemberDelete"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    p_deleted_by VARCHAR,
    INOUT p_response_code INTEGER DEFAULT 0,
    INOUT p_status_code INTEGER DEFAULT 0,
    INOUT p_response_message VARCHAR DEFAULT NULL
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
        p_response_code := 1;
        p_status_code := 404;
        p_response_message := 'Member not found.';
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1 FROM "Members"
        WHERE "FK_Members_Parent" = p_member_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        p_response_code := 2;
        p_status_code := 400;
        p_response_message := 'Cannot delete this member because they have children. Remove or reassign children first.';
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

    p_response_code := 0;
    p_status_code := 200;
    p_response_message := 'Member deleted successfully.';
EXCEPTION WHEN OTHERS THEN
    p_response_code := 99;
    p_status_code := 500;
    p_response_message := SQLERRM;
END;
$$;
