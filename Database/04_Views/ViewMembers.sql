CREATE OR REPLACE VIEW "ViewMembers" AS
SELECT
    m."ID_Members",
    m."FK_Families",
    m."FK_Members_Parent",
    m."FK_Members_Spouse",
    m."FirstName",
    m."LastName",
    (m."FirstName" || ' ' || m."LastName") AS "FullName",
    m."Email",
    m."Phone",
    m."Gender",
    m."DateOfBirth",
    m."DateOfDeath",
    m."IsRoot",
    m."Nickname",
    m."Biography",
    m."Profession",
    m."CreatedOn",
    (
        SELECT i."ImageUrl"
        FROM "MemberImages" i
        WHERE i."FK_Members" = m."ID_Members"
          AND i."IsCancelled" = FALSE
        ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
        LIMIT 1
    ) AS "PhotoUrl"
FROM "Members" m
WHERE m."IsCancelled" = FALSE;
