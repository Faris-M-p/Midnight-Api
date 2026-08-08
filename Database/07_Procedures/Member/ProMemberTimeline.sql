CREATE OR REPLACE PROCEDURE "ProMemberTimeline"(
    "p_FK_Families" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_timeline'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT
        e."ID_MemberEvents" AS "EventId",
        e."FK_Members" AS "MemberId",
        (m."FirstName" || ' ' || m."LastName") AS "MemberName",
        e."EventType" AS "EventType",
        e."Title" AS "Title",
        e."Description" AS "Description",
        e."EventDate" AS "EventDate"
    FROM "MemberEvents" e
    INNER JOIN "Members" m ON m."ID_Members" = e."FK_Members"
    WHERE e."IsCancelled" = FALSE
      AND m."IsCancelled" = FALSE
      AND m."FK_Families" = "p_FK_Families"
    ORDER BY e."EventDate" DESC, e."Title" ASC;
END;
$$;
