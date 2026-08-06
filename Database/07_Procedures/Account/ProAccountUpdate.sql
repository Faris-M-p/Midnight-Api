CREATE OR REPLACE FUNCTION "ProAccountUpdate"(
    p_id            BIGINT,
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash VARCHAR,
    p_updated_by    VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."ID_UserAccounts" = p_id
          AND a."IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 1, 404, 'Account not found.'::VARCHAR;
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = p_username
          AND a."IsCancelled" = FALSE
          AND a."ID_UserAccounts" <> p_id
    ) THEN
        RETURN QUERY SELECT 2, 409, 'Username already exists.'::VARCHAR;
        RETURN;
    END IF;

    UPDATE "UserAccounts" a
    SET "Username" = p_username,
        "Email" = p_email,
        "PasswordHash" = COALESCE(NULLIF(p_password_hash, ''), a."PasswordHash"),
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE a."ID_UserAccounts" = p_id
      AND a."IsCancelled" = FALSE;

    RETURN QUERY SELECT 0, 200, 'Account updated successfully.'::VARCHAR;
END;
$$;
