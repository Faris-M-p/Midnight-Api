CREATE OR REPLACE FUNCTION "ProAccountUpdate"(
    p_id            BIGINT,
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash VARCHAR,
    p_updated_by    VARCHAR
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
BEGIN
    UPDATE "UserAccounts" a
    SET "Username" = p_username,
        "Email" = p_email,
        "PasswordHash" = COALESCE(NULLIF(p_password_hash, ''), a."PasswordHash"),
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE a."ID_UserAccounts" = p_id
      AND a."IsCancelled" = FALSE;

    RETURN QUERY
    SELECT a."ID_UserAccounts", a."FK_Families", a."Username", a."Email", a."IsActive"
    FROM "UserAccounts" a
    WHERE a."ID_UserAccounts" = p_id
      AND a."IsCancelled" = FALSE;
END;
$$;
