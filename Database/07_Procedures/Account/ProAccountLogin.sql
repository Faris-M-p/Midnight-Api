CREATE OR REPLACE PROCEDURE "ProAccountLogin"(
    p_username VARCHAR,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(t) INTO p_payload
    FROM (
        SELECT
            a."ID_UserAccounts",
            a."FK_Families",
            a."Username",
            a."Email",
            a."IsActive",
            a."PasswordHash"
        FROM "UserAccounts" a
        WHERE a."Username" = p_username
          AND a."IsCancelled" = FALSE
          AND a."IsActive" = TRUE
    ) t;
END;
$$;
