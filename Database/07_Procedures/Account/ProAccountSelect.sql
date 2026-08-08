CREATE OR REPLACE PROCEDURE "ProAccountSelect"(
    p_id BIGINT,
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
            a."IsActive"
        FROM "UserAccounts" a
        WHERE a."ID_UserAccounts" = p_id
          AND a."IsCancelled" = FALSE
    ) t;
END;
$$;
