CREATE OR REPLACE PROCEDURE "ProAccountSelect"(
    "p_ID_UserAccounts" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_select'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT
        a."ID_UserAccounts",
        a."FK_Families",
        a."Username",
        a."Email",
        a."IsActive"
    FROM "UserAccounts" a
    WHERE a."ID_UserAccounts" = "p_ID_UserAccounts"
      AND a."IsCancelled" = FALSE;
END;
$$;
