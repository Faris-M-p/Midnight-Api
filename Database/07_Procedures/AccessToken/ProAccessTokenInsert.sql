CREATE OR REPLACE PROCEDURE "ProAccessTokenInsert"(
    "p_FK_Families" BIGINT,
    "p_TokenName" VARCHAR,
    "p_Permission" VARCHAR,
    "p_Scope" VARCHAR,
    "p_FK_Members" BIGINT,
    "p_TokenHash" VARCHAR,
    "p_TokenPreview" VARCHAR,
    "p_ExpiresOn" TIMESTAMPTZ,
    "p_CreatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id BIGINT;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Families"
        WHERE "ID_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Family not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_Scope" IN ('SelectedMember', 'MemberDescendants') THEN
        IF "p_FK_Members" IS NULL OR NOT EXISTS (
            SELECT 1 FROM "Members"
            WHERE "ID_Members" = "p_FK_Members"
              AND "FK_Families" = "p_FK_Families"
              AND "IsCancelled" = FALSE
        ) THEN
            "p_ResponseCode" := -1;
            "p_Status" := FALSE;
            "p_ResponseMessage" := 'Select a valid family member for this scope.';
            "p_Data" := NULL;
            RETURN;
        END IF;
    ELSE
        "p_FK_Members" := NULL;
    END IF;

    IF "p_ExpiresOn" <= NOW() THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Expiry must be in the future.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    INSERT INTO "AccessTokens" (
        "FK_Families", "FK_Members", "TokenName", "Permission", "Scope",
        "TokenHash", "TokenPreview", "Status", "ExpiresOn", "CreatedBy", "CreatedOn"
    )
    VALUES (
        "p_FK_Families", "p_FK_Members", TRIM("p_TokenName"), "p_Permission", "p_Scope",
        "p_TokenHash", "p_TokenPreview", 'Active', "p_ExpiresOn", "p_CreatedBy", NOW()
    )
    RETURNING "ID_AccessTokens" INTO v_id;

    "p_ResponseCode" := v_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Access token generated successfully.';
    "p_Data" := jsonb_build_object('Id', v_id);
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
