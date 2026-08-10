CREATE OR REPLACE PROCEDURE "ProAccessTokenList"(
    "p_FK_Families" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'access_token_list'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT
        t."ID_AccessTokens" AS "Id",
        t."TokenName" AS "TokenName",
        CASE
            WHEN t."ExpiresOn" < NOW() THEN 'Expired'
            ELSE t."Status"
        END AS "Status",
        t."Permission" AS "Permission",
        t."Scope" AS "Scope",
        t."FK_Members" AS "MemberId",
        CASE
            WHEN m."ID_Members" IS NULL THEN NULL
            ELSE TRIM(COALESCE(m."FirstName", '') || ' ' || COALESCE(m."LastName", ''))
        END AS "MemberName",
        t."TokenPreview" AS "TokenPreview",
        t."CreatedOn" AS "CreatedOn",
        t."ExpiresOn" AS "ExpiresOn",
        t."LastUsedOn" AS "LastUsedOn",
        t."ActiveSessions" AS "ActiveSessions",
        t."UsageCount" AS "UsageCount"
    FROM "AccessTokens" t
    LEFT JOIN "Members" m
        ON m."ID_Members" = t."FK_Members"
       AND m."IsCancelled" = FALSE
    WHERE t."FK_Families" = "p_FK_Families"
      AND t."IsCancelled" = FALSE
    ORDER BY t."CreatedOn" DESC, t."ID_AccessTokens" DESC;
END;
$$;
