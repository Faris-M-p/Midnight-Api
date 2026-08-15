CREATE OR REPLACE PROCEDURE "ProAccountOtpIncrementAttempt"(
    "p_ID_AccountOtps" BIGINT,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_attempts INTEGER;
BEGIN
    UPDATE "AccountOtps" o
    SET "AttemptCount" = o."AttemptCount" + 1
    WHERE o."ID_AccountOtps" = "p_ID_AccountOtps"
      AND o."IsCancelled" = FALSE
    RETURNING o."AttemptCount" INTO v_attempts;

    IF v_attempts IS NULL THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'OTP not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    "p_ResponseCode" := "p_ID_AccountOtps";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'OTP attempt recorded.';
    "p_Data" := jsonb_build_object('Id', "p_ID_AccountOtps", 'AttemptCount', v_attempts);
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
