CREATE OR REPLACE PROCEDURE "ProAccessTokenRecordLogin"(
    "p_FK_Families" BIGINT,
    "p_ID_AccessTokens" BIGINT,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "AccessTokens"
    SET "LastUsedOn" = NOW(),
        "UsageCount" = "UsageCount" + 1,
        "UpdatedOn" = NOW()
    WHERE "ID_AccessTokens" = "p_ID_AccessTokens"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    IF NOT FOUND THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Access token not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    "p_ResponseCode" := "p_ID_AccessTokens";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Login recorded.';
    "p_Data" := jsonb_build_object('Id', "p_ID_AccessTokens");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
