CREATE OR REPLACE PROCEDURE "ProAccountRegister"(
    "p_FamilyCode" VARCHAR,
    "p_FamilyName" VARCHAR,
    "p_Description" VARCHAR,
    "p_Username" VARCHAR,
    "p_Email" VARCHAR,
    "p_PasswordHash" VARCHAR,
    "p_CreatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_family_id BIGINT;
    v_account_id BIGINT;
BEGIN
    IF EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = "p_Username"
          AND a."IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 20;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Username already exists.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE LOWER(a."Email") = LOWER("p_Email")
          AND a."IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 20;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Email already exists.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = "p_FamilyCode"
          AND f."IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 20;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Family code already exists.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    INSERT INTO "Families" ("FamilyCode", "FamilyName", "Description", "CreatedBy", "CreatedOn")
    VALUES ("p_FamilyCode", "p_FamilyName", "p_Description", "p_CreatedBy", NOW())
    RETURNING "ID_Families" INTO v_family_id;

    INSERT INTO "UserAccounts" (
        "FK_Families", "Username", "Email", "PasswordHash", "IsActive", "EmailVerified", "CreatedBy", "CreatedOn"
    )
    VALUES (
        v_family_id, "p_Username", "p_Email", "p_PasswordHash", TRUE, FALSE, "p_CreatedBy", NOW()
    )
    RETURNING "ID_UserAccounts" INTO v_account_id;

    "p_ResponseCode" := v_account_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Account registered successfully.';
    "p_Data" := jsonb_build_object('Id', v_account_id, 'FamilyId', v_family_id, 'Email', "p_Email");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
