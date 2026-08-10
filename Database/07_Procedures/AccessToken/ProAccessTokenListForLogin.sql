CREATE OR REPLACE PROCEDURE "ProAccessTokenListForLogin"(
    "p_FK_Families" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'access_token_login_list'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT
        t."ID_AccessTokens" AS "Id",
        t."TokenName" AS "TokenName",
        t."Permission" AS "Permission",
        t."Scope" AS "Scope",
        t."FK_Members" AS "MemberId",
        t."TokenHash" AS "TokenHash",
        t."Status" AS "Status",
        t."ExpiresOn" AS "ExpiresOn"
    FROM "AccessTokens" t
    WHERE t."FK_Families" = "p_FK_Families"
      AND t."IsCancelled" = FALSE
    ORDER BY t."ID_AccessTokens" DESC;
END;
$$;
