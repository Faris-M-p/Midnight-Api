CREATE OR REPLACE PROCEDURE "ProMemberGetRelation"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_get_relation'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT
        m."FK_Members_Parent" AS "ParentId",
        m."FK_Members_Spouse" AS "SpouseId"
    FROM "Members" m
    WHERE m."ID_Members" = "p_ID_Members"
      AND m."FK_Families" = "p_FK_Families"
      AND m."IsCancelled" = FALSE;
END;
$$;
