CREATE OR REPLACE PROCEDURE "ProAccessTokenSelect"(
    "p_FK_Families" BIGINT,
    "p_ID_AccessTokens" BIGINT,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT jsonb_build_object(
        'Id', t."ID_AccessTokens",
        'TokenName', t."TokenName",
        'Status', CASE WHEN t."ExpiresOn" < NOW() THEN 'Expired' ELSE t."Status" END,
        'Permission', t."Permission",
        'Scope', t."Scope",
        'MemberId', t."FK_Members",
        'MemberName', CASE
            WHEN m."ID_Members" IS NULL THEN NULL
            ELSE TRIM(COALESCE(m."FirstName", '') || ' ' || COALESCE(m."LastName", ''))
        END,
        'TokenPreview', t."TokenPreview",
        'CreatedOn', t."CreatedOn",
        'ExpiresOn', t."ExpiresOn",
        'LastUsedOn', t."LastUsedOn",
        'ActiveSessions', t."ActiveSessions",
        'UsageCount', t."UsageCount"
    )
    INTO "p_Data"
    FROM "AccessTokens" t
    LEFT JOIN "Members" m
        ON m."ID_Members" = t."FK_Members"
       AND m."IsCancelled" = FALSE
    WHERE t."ID_AccessTokens" = "p_ID_AccessTokens"
      AND t."FK_Families" = "p_FK_Families"
      AND t."IsCancelled" = FALSE;

    IF "p_Data" IS NULL THEN
        "p_Data" := NULL;
    END IF;
END;
$$;
