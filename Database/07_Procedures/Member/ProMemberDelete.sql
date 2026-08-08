CREATE OR REPLACE PROCEDURE "ProMemberDelete"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_spouse_id BIGINT;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = "p_ID_Members"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Member not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1 FROM "Members"
        WHERE "FK_Members_Parent" = "p_ID_Members"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 20;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Cannot delete this member because they have children. Remove or reassign children first.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    SELECT "FK_Members_Spouse" INTO v_spouse_id
    FROM "Members"
    WHERE "ID_Members" = "p_ID_Members";

    IF v_spouse_id IS NOT NULL THEN
        UPDATE "Members"
        SET "FK_Members_Spouse" = NULL,
            "UpdatedBy" = "p_UpdatedBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = v_spouse_id
          AND "IsCancelled" = FALSE;
    END IF;

    UPDATE "Members"
    SET "FK_Members_Spouse" = NULL,
        "IsCancelled" = TRUE,
        "CancelledBy" = "p_UpdatedBy",
        "CancelledOn" = NOW()
    WHERE "ID_Members" = "p_ID_Members";

    "p_ResponseCode" := "p_ID_Members";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Member deleted successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Members");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
