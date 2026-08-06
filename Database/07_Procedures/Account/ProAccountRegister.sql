CREATE OR REPLACE FUNCTION "ProAccountRegister"(
    p_family_code   VARCHAR,
    p_family_name   VARCHAR,
    p_description   VARCHAR,
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash VARCHAR,
    p_created_by    VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
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
        RETURN QUERY SELECT 1, 409, 'Username already exists.'::VARCHAR;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = p_family_code
          AND f."IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 2, 409, 'Family code already exists.'::VARCHAR;
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

    RETURN QUERY SELECT 0, 201, 'Account registered successfully.'::VARCHAR;
END;
$$;
