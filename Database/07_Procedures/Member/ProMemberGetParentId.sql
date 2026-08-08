CREATE OR REPLACE PROCEDURE "ProMemberGetParentId"(
    "p_ID_Members" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_get_parent_id'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT m."FK_Members_Parent" AS "ParentId"
    FROM "Members" m
    WHERE m."ID_Members" = "p_ID_Members"
      AND m."IsCancelled" = FALSE;
END;
$$;
