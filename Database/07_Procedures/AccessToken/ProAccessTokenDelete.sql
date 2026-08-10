CREATE OR REPLACE PROCEDURE "ProAccessTokenDelete"(
    "p_FK_Families" BIGINT,
    "p_ID_AccessTokens" BIGINT,
    "p_CancelledBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "AccessTokens"
        WHERE "ID_AccessTokens" = "p_ID_AccessTokens"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Access token not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "AccessTokens"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW(),
        "Status" = 'Inactive',
        "UpdatedBy" = "p_CancelledBy",
        "UpdatedOn" = NOW()
    WHERE "ID_AccessTokens" = "p_ID_AccessTokens"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_AccessTokens";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Access token deleted successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_AccessTokens");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
