CREATE OR REPLACE PROCEDURE "ProAccountOtpMarkUsed"(
    "p_ID_AccountOtps" BIGINT,
    "p_ResetTokenHash" VARCHAR DEFAULT NULL,
    "p_ResetTokenExpiresOn" TIMESTAMPTZ DEFAULT NULL,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "AccountOtps" o
    SET "IsUsed" = TRUE,
        "UsedOn" = NOW(),
        "ResetTokenHash" = "p_ResetTokenHash",
        "ResetTokenExpiresOn" = "p_ResetTokenExpiresOn"
    WHERE o."ID_AccountOtps" = "p_ID_AccountOtps"
      AND o."IsCancelled" = FALSE
      AND o."IsUsed" = FALSE;

    IF NOT FOUND THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'OTP not found or already used.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    "p_ResponseCode" := "p_ID_AccountOtps";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'OTP marked as used.';
    "p_Data" := jsonb_build_object('Id', "p_ID_AccountOtps");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
