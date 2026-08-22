CREATE OR REPLACE PROCEDURE "ProEventSelect"(
    "p_FK_Families" BIGINT,
    "p_ID_Events" BIGINT,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT jsonb_build_object(
        'Id', e."ID_Events",
        'Title', e."Title",
        'EventType', e."EventType",
        'EventDate', e."EventDate",
        'EventTime', CASE
            WHEN e."EventTime" IS NULL THEN NULL
            ELSE to_char(e."EventTime", 'HH24:MI')
        END,
        'Location', e."Location",
        'Description', e."Description",
        'Members', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', m."ID_Members",
                    'FirstName', m."FirstName",
                    'LastName', m."LastName",
                    'FullName', TRIM(CONCAT(COALESCE(m."FirstName", ''), ' ', COALESCE(m."LastName", ''))),
                    'PhotoUrl', (
                        SELECT img."ImageUrl"
                        FROM "MemberImages" img
                        WHERE img."FK_Members" = m."ID_Members"
                          AND img."IsCancelled" = FALSE
                        ORDER BY img."IsPrimary" DESC, img."SortOrder" ASC, img."ID_MemberImages" ASC
                        LIMIT 1
                    )
                )
                ORDER BY LOWER(m."FirstName"), LOWER(m."LastName"), m."ID_Members"
            )
            FROM "EventMembers" em
            INNER JOIN "Members" m
                ON m."ID_Members" = em."FK_Members"
               AND m."FK_Families" = e."FK_Families"
               AND m."IsCancelled" = FALSE
            WHERE em."FK_Events" = e."ID_Events"
              AND em."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'CreatedOn', e."CreatedOn",
        'UpdatedOn', e."UpdatedOn"
    )
    INTO "p_Data"
    FROM "Events" e
    WHERE e."ID_Events" = "p_ID_Events"
      AND e."FK_Families" = "p_FK_Families"
      AND e."IsCancelled" = FALSE;

    IF "p_Data" IS NULL THEN
        "p_Data" := NULL;
    END IF;
END;
$$;
