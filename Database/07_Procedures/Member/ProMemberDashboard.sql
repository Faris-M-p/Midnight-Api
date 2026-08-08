CREATE OR REPLACE PROCEDURE "ProMemberDashboard"(
    p_family_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_root_id BIGINT;
    v_gens INTEGER := 0;
    v_today DATE := CURRENT_DATE;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Families"
        WHERE "ID_Families" = p_family_id AND "IsCancelled" = FALSE
    ) THEN
        p_payload := NULL;
        RETURN;
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

    p_payload := jsonb_build_object(
        'Family', (
            SELECT jsonb_build_object(
                'ID_Families', f."ID_Families",
                'FamilyCode', f."FamilyCode",
                'FamilyName', f."FamilyName",
                'Description', f."Description"
            )
            FROM "Families" f
            WHERE f."ID_Families" = p_family_id
        ),
        'TotalMembers', (
            SELECT COUNT(*)::INTEGER
            FROM "Members" m
            WHERE m."FK_Families" = p_family_id AND m."IsCancelled" = FALSE
        ),
        'TotalGenerations', v_gens,
        'RecentMembers', COALESCE((
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
        'UpcomingBirthdays', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'MemberId', b."MemberId",
                    'FullName', b."FullName",
                    'DateOfBirth', b."DateOfBirth",
                    'TurningAge', b."TurningAge",
                    'DaysUntil', b."DaysUntil"
                )
                ORDER BY b."DaysUntil"
            )
            FROM (
                SELECT
                    m."ID_Members" AS "MemberId",
                    (m."FirstName" || ' ' || m."LastName") AS "FullName",
                    m."DateOfBirth",
                    (EXTRACT(YEAR FROM age(nb.next_bday, m."DateOfBirth")))::INTEGER AS "TurningAge",
                    (nb.next_bday - v_today) AS "DaysUntil"
                FROM "Members" m
                CROSS JOIN LATERAL (
                    SELECT
                        CASE
                            WHEN make_date(
                                EXTRACT(YEAR FROM v_today)::INT,
                                EXTRACT(MONTH FROM m."DateOfBirth")::INT,
                                LEAST(
                                    EXTRACT(DAY FROM m."DateOfBirth")::INT,
                                    EXTRACT(DAY FROM (DATE_TRUNC('month', make_date(
                                        EXTRACT(YEAR FROM v_today)::INT,
                                        EXTRACT(MONTH FROM m."DateOfBirth")::INT,
                                        1
                                    )) + INTERVAL '1 month - 1 day'))::INT
                                )
                            ) < v_today
                            THEN make_date(
                                EXTRACT(YEAR FROM v_today)::INT + 1,
                                EXTRACT(MONTH FROM m."DateOfBirth")::INT,
                                LEAST(
                                    EXTRACT(DAY FROM m."DateOfBirth")::INT,
                                    EXTRACT(DAY FROM (DATE_TRUNC('month', make_date(
                                        EXTRACT(YEAR FROM v_today)::INT + 1,
                                        EXTRACT(MONTH FROM m."DateOfBirth")::INT,
                                        1
                                    )) + INTERVAL '1 month - 1 day'))::INT
                                )
                            )
                            ELSE make_date(
                                EXTRACT(YEAR FROM v_today)::INT,
                                EXTRACT(MONTH FROM m."DateOfBirth")::INT,
                                LEAST(
                                    EXTRACT(DAY FROM m."DateOfBirth")::INT,
                                    EXTRACT(DAY FROM (DATE_TRUNC('month', make_date(
                                        EXTRACT(YEAR FROM v_today)::INT,
                                        EXTRACT(MONTH FROM m."DateOfBirth")::INT,
                                        1
                                    )) + INTERVAL '1 month - 1 day'))::INT
                                )
                            )
                        END AS next_bday
                ) nb
                WHERE m."FK_Families" = p_family_id
                  AND m."IsCancelled" = FALSE
                  AND m."DateOfBirth" IS NOT NULL
                  AND m."DateOfDeath" IS NULL
                ORDER BY nb.next_bday
                LIMIT 5
            ) b
        ), '[]'::jsonb),
        'Stats', (
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
        )
    );
END;
$$;
