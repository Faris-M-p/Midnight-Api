CREATE OR REPLACE PROCEDURE "ProEventList"(
    "p_FK_Families" BIGINT,
    "p_Search" VARCHAR DEFAULT NULL,
    "p_SortBy" VARCHAR DEFAULT 'date',
    "p_Page" INTEGER DEFAULT 1,
    "p_PageSize" INTEGER DEFAULT 100,
    INOUT "p_Result" REFCURSOR DEFAULT 'event_list',
    INOUT "p_TotalCount" INTEGER DEFAULT 0
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_page INTEGER := GREATEST(COALESCE("p_Page", 1), 1);
    v_size INTEGER := LEAST(GREATEST(COALESCE("p_PageSize", 100), 1), 200);
    v_term TEXT := NULLIF(LOWER(TRIM(COALESCE("p_Search", ''))), '');
    v_sort TEXT := LOWER(COALESCE("p_SortBy", 'date'));
BEGIN
    WITH filtered AS (
        SELECT e."ID_Events"
        FROM "Events" e
        WHERE e."FK_Families" = "p_FK_Families"
          AND e."IsCancelled" = FALSE
          AND (
              v_term IS NULL
              OR LOWER(e."Title") LIKE '%' || v_term || '%'
              OR LOWER(e."EventType") LIKE '%' || v_term || '%'
              OR LOWER(COALESCE(e."Location", '')) LIKE '%' || v_term || '%'
              OR LOWER(COALESCE(e."Description", '')) LIKE '%' || v_term || '%'
          )
    )
    SELECT COUNT(*)::INTEGER INTO "p_TotalCount" FROM filtered;

    OPEN "p_Result" FOR
    SELECT
        e."ID_Events" AS "Id",
        e."Title" AS "Title",
        e."EventType" AS "EventType",
        e."EventDate" AS "EventDate",
        CASE
            WHEN e."EventTime" IS NULL THEN NULL
            ELSE to_char(e."EventTime", 'HH24:MI')
        END AS "EventTime",
        e."Location" AS "Location",
        e."Description" AS "Description",
        (
            SELECT COUNT(*)::INTEGER
            FROM "EventMembers" em
            WHERE em."FK_Events" = e."ID_Events"
              AND em."IsCancelled" = FALSE
        ) AS "MemberCount",
        e."CreatedOn" AS "CreatedOn"
    FROM "Events" e
    WHERE e."FK_Families" = "p_FK_Families"
      AND e."IsCancelled" = FALSE
      AND (
          v_term IS NULL
          OR LOWER(e."Title") LIKE '%' || v_term || '%'
          OR LOWER(e."EventType") LIKE '%' || v_term || '%'
          OR LOWER(COALESCE(e."Location", '')) LIKE '%' || v_term || '%'
          OR LOWER(COALESCE(e."Description", '')) LIKE '%' || v_term || '%'
      )
    ORDER BY
        CASE WHEN v_sort = 'title' THEN LOWER(e."Title") END ASC NULLS LAST,
        CASE WHEN v_sort = 'recent' THEN e."EventDate" END DESC NULLS LAST,
        CASE WHEN v_sort = 'recent' THEN e."EventTime" END DESC NULLS LAST,
        CASE WHEN v_sort = 'recent' THEN e."ID_Events" END DESC,
        CASE WHEN v_sort IS DISTINCT FROM 'title' AND v_sort IS DISTINCT FROM 'recent' THEN e."EventDate" END ASC NULLS LAST,
        CASE WHEN v_sort IS DISTINCT FROM 'title' AND v_sort IS DISTINCT FROM 'recent' THEN e."EventTime" END ASC NULLS LAST,
        CASE WHEN v_sort IS DISTINCT FROM 'title' AND v_sort IS DISTINCT FROM 'recent' THEN e."ID_Events" END ASC
    OFFSET (v_page - 1) * v_size
    LIMIT v_size;
END;
$$;
