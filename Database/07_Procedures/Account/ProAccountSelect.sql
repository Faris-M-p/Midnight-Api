CREATE OR REPLACE FUNCTION "ProAccountSelect"(
    p_id BIGINT
)
RETURNS TABLE (
    "ID_UserAccounts" BIGINT,
    "FK_Families"     BIGINT,
    "Username"        VARCHAR,
    "Email"           VARCHAR,
    "IsActive"        BOOLEAN
)
LANGUAGE sql
AS $$
    SELECT a."ID_UserAccounts", a."FK_Families", a."Username", a."Email", a."IsActive"
    FROM "UserAccounts" a
    WHERE a."ID_UserAccounts" = p_id
      AND a."IsCancelled" = FALSE;
$$;
