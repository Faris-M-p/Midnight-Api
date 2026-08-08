CREATE OR REPLACE PROCEDURE "ProAccountExistsByUsername"(
    "p_Username" VARCHAR,
    "p_ID_UserAccounts" BIGINT DEFAULT NULL,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_exists_by_username'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT EXISTS (
        SELECT 1
        FROM "UserAccounts" a
        WHERE a."Username" = "p_Username"
          AND a."IsCancelled" = FALSE
          AND ("p_ID_UserAccounts" IS NULL OR a."ID_UserAccounts" <> "p_ID_UserAccounts")
    ) AS "Exists";
END;
$$;
