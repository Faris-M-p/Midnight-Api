CREATE OR REPLACE PROCEDURE "ProMemberTimeline"(
    p_family_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT COALESCE(jsonb_agg(to_jsonb(t) ORDER BY t."EventDate" DESC, t."Title" ASC), '[]'::jsonb)
    INTO p_payload
    FROM (
        SELECT
            e."ID_MemberEvents" AS "EventId",
            e."FK_Members" AS "MemberId",
            (m."FirstName" || ' ' || m."LastName") AS "MemberName",
            e."EventType",
            e."Title",
            e."Description",
            e."EventDate"
        FROM "MemberEvents" e
        INNER JOIN "Members" m ON m."ID_Members" = e."FK_Members"
        WHERE e."IsCancelled" = FALSE
          AND m."IsCancelled" = FALSE
          AND m."FK_Families" = p_family_id
    ) t;
END;
$$;
