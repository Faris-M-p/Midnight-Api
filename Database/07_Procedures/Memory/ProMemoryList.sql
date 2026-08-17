CREATE OR REPLACE PROCEDURE "ProMemoryList"(
    "p_FK_Families" BIGINT,
    "p_Search" VARCHAR DEFAULT NULL,
    "p_SortBy" VARCHAR DEFAULT 'recent',
    "p_Page" INTEGER DEFAULT 1,
    "p_PageSize" INTEGER DEFAULT 12,
    INOUT "p_Result" REFCURSOR DEFAULT 'memory_list',
    INOUT "p_TotalCount" INTEGER DEFAULT 0
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_page INTEGER := GREATEST(COALESCE("p_Page", 1), 1);
    v_size INTEGER := LEAST(GREATEST(COALESCE("p_PageSize", 12), 1), 50);
    v_term TEXT := NULLIF(LOWER(TRIM(COALESCE("p_Search", ''))), '');
    v_sort TEXT := LOWER(COALESCE("p_SortBy", 'recent'));
BEGIN
    WITH filtered AS (
        SELECT m."ID_Memories"
        FROM "Memories" m
        WHERE m."FK_Families" = "p_FK_Families"
          AND m."IsCancelled" = FALSE
          AND (
              v_term IS NULL
              OR LOWER(m."Title") LIKE '%' || v_term || '%'
              OR LOWER(COALESCE(m."Description", '')) LIKE '%' || v_term || '%'
          )
    )
    SELECT COUNT(*)::INTEGER INTO "p_TotalCount" FROM filtered;

    OPEN "p_Result" FOR
    SELECT
        m."ID_Memories" AS "Id",
        m."Title" AS "Title",
        m."Description" AS "Description",
        m."MemoryDate" AS "MemoryDate",
        m."Location" AS "Location",
        m."CoverImageUrl" AS "CoverImageUrl",
        m."CoverImageUrl" AS "CoverUrl",
        (
            SELECT img."ID_MemoryImages"
            FROM "MemoryImages" img
            WHERE img."FK_Memories" = m."ID_Memories"
              AND img."IsCancelled" = FALSE
              AND img."IsCoverImage" = TRUE
            ORDER BY img."ID_MemoryImages"
            LIMIT 1
        ) AS "CoverImageId",
        (
            SELECT COUNT(*)::INTEGER
            FROM "MemoryImages" img
            WHERE img."FK_Memories" = m."ID_Memories"
              AND img."IsCancelled" = FALSE
        ) AS "ImageCount",
        m."CreatedOn" AS "CreatedOn"
    FROM "Memories" m
    WHERE m."FK_Families" = "p_FK_Families"
      AND m."IsCancelled" = FALSE
      AND (
          v_term IS NULL
          OR LOWER(m."Title") LIKE '%' || v_term || '%'
          OR LOWER(COALESCE(m."Description", '')) LIKE '%' || v_term || '%'
      )
    ORDER BY
        CASE WHEN v_sort = 'title' THEN LOWER(m."Title") END ASC NULLS LAST,
        CASE WHEN v_sort = 'oldest' THEN m."MemoryDate" END ASC NULLS LAST,
        CASE WHEN v_sort = 'oldest' THEN m."ID_Memories" END ASC,
        CASE WHEN v_sort IS DISTINCT FROM 'title' AND v_sort IS DISTINCT FROM 'oldest' THEN m."MemoryDate" END DESC NULLS LAST,
        CASE WHEN v_sort IS DISTINCT FROM 'title' AND v_sort IS DISTINCT FROM 'oldest' THEN m."ID_Memories" END DESC
    OFFSET (v_page - 1) * v_size
    LIMIT v_size;
END;
$$;
