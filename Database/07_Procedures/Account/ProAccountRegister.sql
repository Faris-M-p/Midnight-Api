CREATE OR REPLACE FUNCTION "ProAccountRegister"(
    p_fk_families   BIGINT,
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash VARCHAR,
    p_is_active     BOOLEAN,
    p_created_by    VARCHAR
)
RETURNS TABLE (
    "ID_UserAccounts" BIGINT,
    "FK_Families"     BIGINT,
    "Username"        VARCHAR,
    "Email"           VARCHAR,
    "IsActive"        BOOLEAN
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id BIGINT;
BEGIN
    INSERT INTO "UserAccounts" (
        "FK_Families", "Username", "Email", "PasswordHash", "IsActive", "CreatedBy", "CreatedOn"
    )
    VALUES (
        p_fk_families, p_username, p_email, p_password_hash, p_is_active, p_created_by, NOW()
    )
    RETURNING "UserAccounts"."ID_UserAccounts" INTO v_id;

    RETURN QUERY
    SELECT a."ID_UserAccounts", a."FK_Families", a."Username", a."Email", a."IsActive"
    FROM "UserAccounts" a
    WHERE a."ID_UserAccounts" = v_id;
END;
$$;
