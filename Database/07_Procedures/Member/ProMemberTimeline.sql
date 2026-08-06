CREATE OR REPLACE FUNCTION "ProMemberTimeline"(
    p_family_id BIGINT
)
RETURNS TABLE (
    "EventId"     BIGINT,
    "MemberId"    BIGINT,
    "MemberName"  TEXT,
    "EventType"   VARCHAR,
    "Title"       VARCHAR,
    "Description" VARCHAR,
    "EventDate"   DATE
)
LANGUAGE sql
AS $$
    SELECT
        e."ID_MemberEvents",
        e."FK_Members",
        (m."FirstName" || ' ' || m."LastName"),
        e."EventType",
        e."Title",
        e."Description",
        e."EventDate"
    FROM "MemberEvents" e
    INNER JOIN "Members" m ON m."ID_Members" = e."FK_Members"
    WHERE e."IsCancelled" = FALSE
      AND m."IsCancelled" = FALSE
      AND m."FK_Families" = p_family_id
    ORDER BY e."EventDate" DESC, e."Title" ASC;
$$;
