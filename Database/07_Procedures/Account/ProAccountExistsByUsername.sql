CREATE OR REPLACE FUNCTION "ProAccountExistsByUsername"(
    p_username   VARCHAR,
    p_exclude_id BIGINT DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = p_username
          AND a."IsCancelled" = FALSE
          AND (p_exclude_id IS NULL OR a."ID_UserAccounts" <> p_exclude_id)
    );
$$;
