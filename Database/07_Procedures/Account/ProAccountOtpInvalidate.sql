CREATE OR REPLACE PROCEDURE "ProAccountOtpInvalidate"(
    "p_FK_UserAccounts" BIGINT,
    "p_Purpose" VARCHAR,
    "p_CancelledBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "AccountOtps" o
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW()
    WHERE o."FK_UserAccounts" = "p_FK_UserAccounts"
      AND o."Purpose" = "p_Purpose"
      AND o."IsCancelled" = FALSE
      AND o."IsUsed" = FALSE;

    "p_ResponseCode" := "p_FK_UserAccounts";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'OTP records invalidated.';
    "p_Data" := jsonb_build_object('Id', "p_FK_UserAccounts");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
