CREATE OR REPLACE FUNCTION "ProMemberDashboard"(
    p_family_id BIGINT
)
RETURNS TABLE (
    "FamilyJson"            JSONB,
    "TotalMembers"          INTEGER,
    "TotalGenerations"      INTEGER,
    "RecentMembersJson"     JSONB,
    "MembersForBirthdayJson" JSONB,
    "StatsJson"             JSONB
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_root_id BIGINT;
    v_gens INTEGER := 0;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Families"
        WHERE "ID_Families" = p_family_id AND "IsCancelled" = FALSE
    ) THEN
        RAISE EXCEPTION 'Family not found.';
    END IF;

    SELECT m."ID_Members" INTO v_root_id
    FROM "Members" m
    WHERE m."FK_Families" = p_family_id
      AND m."IsRoot" = TRUE
      AND m."IsCancelled" = FALSE
    LIMIT 1;

    IF v_root_id IS NOT NULL THEN
        WITH RECURSIVE tree AS (
            SELECT m."ID_Members", 1 AS depth
            FROM "Members" m
            WHERE m."ID_Members" = v_root_id
            UNION ALL
            SELECT c."ID_Members", t.depth + 1
            FROM "Members" c
            INNER JOIN tree t ON c."FK_Members_Parent" = t."ID_Members"
            WHERE c."IsCancelled" = FALSE
              AND c."FK_Families" = p_family_id
        )
        SELECT COALESCE(MAX(depth), 0) INTO v_gens FROM tree;
    END IF;

    RETURN QUERY
    SELECT
        (
            SELECT jsonb_build_object(
                'ID_Families', f."ID_Families",
                'FamilyCode', f."FamilyCode",
                'FamilyName', f."FamilyName",
                'Description', f."Description"
            )
            FROM "Families" f
            WHERE f."ID_Families" = p_family_id
        ),
        (
            SELECT COUNT(*)::INTEGER
            FROM "Members" m
            WHERE m."FK_Families" = p_family_id AND m."IsCancelled" = FALSE
        ),
        v_gens,
        COALESCE((
            SELECT jsonb_agg(row_to_json(x)::jsonb)
            FROM (
                SELECT
                    m."ID_Members" AS "Id",
                    m."FirstName",
                    m."LastName",
                    (m."FirstName" || ' ' || m."LastName") AS "FullName",
                    m."Gender",
                    m."DateOfBirth",
                    m."IsRoot",
                    m."Profession",
                    (
                        SELECT i."ImageUrl"
                        FROM "MemberImages" i
                        WHERE i."FK_Members" = m."ID_Members" AND i."IsCancelled" = FALSE
                        ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
                        LIMIT 1
                    ) AS "PhotoUrl"
                FROM "Members" m
                WHERE m."FK_Families" = p_family_id AND m."IsCancelled" = FALSE
                ORDER BY m."CreatedOn" DESC
                LIMIT 5
            ) x
        ), '[]'::jsonb),
        COALESCE((
            SELECT jsonb_agg(row_to_json(b)::jsonb)
            FROM (
                SELECT
                    m."ID_Members" AS "MemberId",
                    (m."FirstName" || ' ' || m."LastName") AS "FullName",
                    m."DateOfBirth"
                FROM "Members" m
                WHERE m."FK_Families" = p_family_id
                  AND m."IsCancelled" = FALSE
                  AND m."DateOfBirth" IS NOT NULL
                  AND m."DateOfDeath" IS NULL
            ) b
        ), '[]'::jsonb),
        (
            SELECT jsonb_build_object(
                'MaleCount', COUNT(*) FILTER (WHERE LOWER(COALESCE(m."Gender", '')) = 'male'),
                'FemaleCount', COUNT(*) FILTER (WHERE LOWER(COALESCE(m."Gender", '')) = 'female'),
                'OtherGenderCount', COUNT(*) FILTER (
                    WHERE LOWER(COALESCE(m."Gender", '')) NOT IN ('male', 'female')
                ),
                'LivingCount', COUNT(*) FILTER (WHERE m."DateOfDeath" IS NULL),
                'DeceasedCount', COUNT(*) FILTER (WHERE m."DateOfDeath" IS NOT NULL)
            )
            FROM "Members" m
            WHERE m."FK_Families" = p_family_id AND m."IsCancelled" = FALSE
        );
END;
$$;
