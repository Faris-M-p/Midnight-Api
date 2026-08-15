CREATE OR REPLACE PROCEDURE "ProAccountOtpInsert"(
    "p_FK_UserAccounts" BIGINT,
    "p_Email" VARCHAR,
    "p_OtpHash" VARCHAR,
    "p_Purpose" VARCHAR,
    "p_ExpiresOn" TIMESTAMPTZ,
    "p_MaxAttempts" INTEGER,
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
    INSERT INTO "AccountOtps" (
        "FK_UserAccounts",
        "Email",
        "OtpHash",
        "Purpose",
        "ExpiresOn",
        "MaxAttempts",
        "IsUsed",
        "CreatedOn",
        "LastSentOn"
    )
    VALUES (
        "p_FK_UserAccounts",
        "p_Email",
        "p_OtpHash",
        "p_Purpose",
        "p_ExpiresOn",
        COALESCE("p_MaxAttempts", 5),
        FALSE,
        NOW(),
        NOW()
    )
    RETURNING "ID_AccountOtps" INTO v_id;

    "p_ResponseCode" := v_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'OTP created successfully.';
    "p_Data" := jsonb_build_object('Id', v_id);
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
