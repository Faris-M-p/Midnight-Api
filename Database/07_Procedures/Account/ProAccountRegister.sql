CREATE OR REPLACE PROCEDURE "ProAccountRegister"(
    p_family_code   VARCHAR,
    p_family_name   VARCHAR,
    p_description   VARCHAR,
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash VARCHAR,
    p_created_by    VARCHAR,
    INOUT p_response_code INTEGER DEFAULT 0,
    INOUT p_status_code INTEGER DEFAULT 0,
    INOUT p_response_message VARCHAR DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_family_id BIGINT;
BEGIN
    IF EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = p_username
          AND a."IsCancelled" = FALSE
    ) THEN
        p_response_code := 1;
        p_status_code := 409;
        p_response_message := 'Username already exists.';
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = p_family_code
          AND f."IsCancelled" = FALSE
    ) THEN
        p_response_code := 2;
        p_status_code := 409;
        p_response_message := 'Family code already exists.';
        RETURN;
    END IF;

    INSERT INTO "Families" ("FamilyCode", "FamilyName", "Description", "CreatedBy", "CreatedOn")
    VALUES (p_family_code, p_family_name, p_description, p_created_by, NOW())
    RETURNING "Families"."ID_Families" INTO v_family_id;

    INSERT INTO "UserAccounts" (
        "FK_Families", "Username", "Email", "PasswordHash", "IsActive", "CreatedBy", "CreatedOn"
    )
    VALUES (
        v_family_id, p_username, p_email, p_password_hash, TRUE, p_created_by, NOW()
    );

    p_response_code := 0;
    p_status_code := 201;
    p_response_message := 'Account registered successfully.';
EXCEPTION WHEN OTHERS THEN
    p_response_code := 99;
    p_status_code := 500;
    p_response_message := SQLERRM;
END;
$$;
