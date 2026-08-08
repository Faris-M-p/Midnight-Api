CREATE OR REPLACE PROCEDURE "ProAccountLogin"(
    "p_Username" VARCHAR,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_login'
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
        a."IsActive",
        a."PasswordHash"
    FROM "UserAccounts" a
    WHERE a."Username" = "p_Username"
      AND a."IsCancelled" = FALSE
      AND a."IsActive" = TRUE;
END;
$$;
