CREATE OR REPLACE PROCEDURE "ProMemberExistsInFamily"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_exists_in_family'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT EXISTS (
        SELECT 1
        FROM "Members" m
        WHERE m."ID_Members" = "p_ID_Members"
          AND m."FK_Families" = "p_FK_Families"
          AND m."IsCancelled" = FALSE
    ) AS "Exists";
END;
$$;
