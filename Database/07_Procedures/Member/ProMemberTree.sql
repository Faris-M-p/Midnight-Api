CREATE OR REPLACE FUNCTION "ProMemberTree"(
    p_family_id BIGINT
)
RETURNS TABLE (
    "Id"              BIGINT,
    "FirstName"       VARCHAR,
    "LastName"        VARCHAR,
    "FullName"        TEXT,
    "Gender"          VARCHAR,
    "DateOfBirth"     DATE,
    "DateOfDeath"     DATE,
    "IsRoot"          BOOLEAN,
    "Nickname"        VARCHAR,
    "PhotoUrl"        VARCHAR,
    "ParentId"        BIGINT,
    "SpouseId"        BIGINT,
    "TotalMembers"    INTEGER
)
LANGUAGE sql
AS $$
    SELECT
        m."ID_Members",
        m."FirstName",
        m."LastName",
        (m."FirstName" || ' ' || m."LastName"),
        m."Gender",
        m."DateOfBirth",
        m."DateOfDeath",
        m."IsRoot",
        m."Nickname",
        (
            SELECT i."ImageUrl"
            FROM "MemberImages" i
            WHERE i."FK_Members" = m."ID_Members"
              AND i."IsCancelled" = FALSE
            ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
            LIMIT 1
        ),
        m."FK_Members_Parent",
        m."FK_Members_Spouse",
        (
            SELECT COUNT(*)::INTEGER
            FROM "Members" t
            WHERE t."FK_Families" = p_family_id
              AND t."IsCancelled" = FALSE
        )
    FROM "Members" m
    WHERE m."FK_Families" = p_family_id
      AND m."IsCancelled" = FALSE
    ORDER BY m."IsRoot" DESC, m."ID_Members";
$$;
