CREATE OR REPLACE PROCEDURE "ProAccountSetEmailVerified"(
    "p_ID_UserAccounts" BIGINT,
    "p_EmailVerified" BOOLEAN,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "UserAccounts" a
    SET "EmailVerified" = "p_EmailVerified",
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE a."ID_UserAccounts" = "p_ID_UserAccounts"
      AND a."IsCancelled" = FALSE;

    IF NOT FOUND THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Account not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    "p_ResponseCode" := "p_ID_UserAccounts";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Email verification status updated.';
    "p_Data" := jsonb_build_object('Id', "p_ID_UserAccounts");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
