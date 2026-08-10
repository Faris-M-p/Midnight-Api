CREATE OR REPLACE PROCEDURE "ProAccessTokenSetStatus"(
    "p_FK_Families" BIGINT,
    "p_ID_AccessTokens" BIGINT,
    "p_NewStatus" VARCHAR,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_expires TIMESTAMPTZ;
BEGIN
    SELECT "ExpiresOn"
    INTO v_expires
    FROM "AccessTokens"
    WHERE "ID_AccessTokens" = "p_ID_AccessTokens"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    IF v_expires IS NULL THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Access token not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_NewStatus" NOT IN ('Active', 'Inactive') THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Invalid token status.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_NewStatus" = 'Active' AND v_expires < NOW() THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Expired tokens cannot be reactivated.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "AccessTokens"
    SET "Status" = "p_NewStatus",
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_AccessTokens" = "p_ID_AccessTokens"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_AccessTokens";
    "p_Status" := TRUE;
    "p_ResponseMessage" := CASE
        WHEN "p_NewStatus" = 'Active' THEN 'Access token activated successfully.'
        ELSE 'Access token deactivated successfully.'
    END;
    "p_Data" := jsonb_build_object('Id', "p_ID_AccessTokens");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
