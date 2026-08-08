CREATE OR REPLACE PROCEDURE "ProAccountUpdate"(
    "p_ID_UserAccounts" BIGINT,
    "p_Username" VARCHAR,
    "p_Email" VARCHAR,
    "p_PasswordHash" VARCHAR,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."ID_UserAccounts" = "p_ID_UserAccounts"
          AND a."IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Account not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = "p_Username"
          AND a."IsCancelled" = FALSE
          AND a."ID_UserAccounts" <> "p_ID_UserAccounts"
    ) THEN
        "p_ResponseCode" := 20;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Username already exists.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "UserAccounts" a
    SET "Username" = "p_Username",
        "Email" = "p_Email",
        "PasswordHash" = COALESCE(NULLIF("p_PasswordHash", ''), a."PasswordHash"),
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE a."ID_UserAccounts" = "p_ID_UserAccounts"
      AND a."IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_UserAccounts";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Account updated successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_UserAccounts");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
