CREATE OR REPLACE PROCEDURE "ProAccountSelectByEmail"(
    "p_Email" VARCHAR,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_by_email'
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
        a."EmailVerified",
        a."PasswordHash"
    FROM "UserAccounts" a
    WHERE LOWER(a."Email") = LOWER("p_Email")
      AND a."IsCancelled" = FALSE
      AND a."IsActive" = TRUE
    ORDER BY a."ID_UserAccounts"
    LIMIT 1;
END;
$$;
