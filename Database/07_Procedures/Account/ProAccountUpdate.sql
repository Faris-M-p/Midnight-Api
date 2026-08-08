CREATE OR REPLACE PROCEDURE "ProAccountUpdate"(
    p_id            BIGINT,
    p_username      VARCHAR,
    p_email         VARCHAR,
    p_password_hash VARCHAR,
    p_updated_by    VARCHAR,
    INOUT p_response_code INTEGER DEFAULT 0,
    INOUT p_status_code INTEGER DEFAULT 0,
    INOUT p_response_message VARCHAR DEFAULT NULL
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
        p_response_code := 1;
        p_status_code := 404;
        p_response_message := 'Account not found.';
        RETURN;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = p_username
          AND a."IsCancelled" = FALSE
          AND a."ID_UserAccounts" <> p_id
    ) THEN
        p_response_code := 2;
        p_status_code := 409;
        p_response_message := 'Username already exists.';
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

    p_response_code := 0;
    p_status_code := 200;
    p_response_message := 'Account updated successfully.';
EXCEPTION WHEN OTHERS THEN
    p_response_code := 99;
    p_status_code := 500;
    p_response_message := SQLERRM;
END;
$$;
