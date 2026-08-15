CREATE OR REPLACE PROCEDURE "ProAccountOtpClearResetToken"(
    "p_ID_AccountOtps" BIGINT,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "AccountOtps" o
    SET "ResetTokenHash" = NULL,
        "ResetTokenExpiresOn" = NULL
    WHERE o."ID_AccountOtps" = "p_ID_AccountOtps"
      AND o."IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_AccountOtps";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Reset token cleared.';
    "p_Data" := jsonb_build_object('Id', "p_ID_AccountOtps");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
