CREATE OR REPLACE FUNCTION "ProAccountLogin"(
    p_username VARCHAR
)
RETURNS TABLE (
    "ID_UserAccounts" BIGINT,
    "FK_Families"     BIGINT,
    "Username"        VARCHAR,
    "Email"           VARCHAR,
    "IsActive"        BOOLEAN,
    "PasswordHash"    VARCHAR
)
LANGUAGE sql
AS $$
    SELECT a."ID_UserAccounts", a."FK_Families", a."Username", a."Email", a."IsActive", a."PasswordHash"
    FROM "UserAccounts" a
    WHERE a."Username" = p_username
      AND a."IsCancelled" = FALSE;
$$;
