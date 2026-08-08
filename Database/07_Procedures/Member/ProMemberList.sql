CREATE OR REPLACE PROCEDURE "ProMemberList"(
    "p_FK_Families" BIGINT,
    "p_Search" VARCHAR DEFAULT NULL,
    "p_Gender" VARCHAR DEFAULT NULL,
    "p_SortBy" VARCHAR DEFAULT 'firstname',
    "p_SortDesc" BOOLEAN DEFAULT FALSE,
    "p_Page" INTEGER DEFAULT 1,
    "p_PageSize" INTEGER DEFAULT 20,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_list',
    INOUT "p_TotalCount" INTEGER DEFAULT 0
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_page INTEGER := GREATEST(COALESCE("p_Page", 1), 1);
    v_size INTEGER := LEAST(GREATEST(COALESCE("p_PageSize", 20), 1), 100);
    v_term TEXT := NULLIF(LOWER(TRIM(COALESCE("p_Search", ''))), '');
    v_sort TEXT := LOWER(COALESCE("p_SortBy", 'firstname'));
BEGIN
    WITH filtered AS (
        SELECT m."ID_Members"
        FROM "Members" m
        WHERE m."FK_Families" = "p_FK_Families"
          AND m."IsCancelled" = FALSE
          AND (v_term IS NULL
               OR LOWER(m."FirstName") LIKE '%' || v_term || '%'
               OR LOWER(m."LastName") LIKE '%' || v_term || '%'
               OR LOWER(m."FirstName" || ' ' || m."LastName") LIKE '%' || v_term || '%')
          AND ("p_Gender" IS NULL OR "p_Gender" = '' OR m."Gender" = "p_Gender")
    )
    SELECT COUNT(*)::INTEGER INTO "p_TotalCount" FROM filtered;

    OPEN "p_Result" FOR
    SELECT
        m."ID_Members" AS "Id",
        m."FirstName" AS "FirstName",
        m."LastName" AS "LastName",
        (m."FirstName" || ' ' || m."LastName") AS "FullName",
        m."Gender" AS "Gender",
        m."DateOfBirth" AS "DateOfBirth",
        m."IsRoot" AS "IsRoot",
        m."Profession" AS "Profession",
        (
            SELECT i."ImageUrl"
            FROM "MemberImages" i
            WHERE i."FK_Members" = m."ID_Members"
              AND i."IsCancelled" = FALSE
            ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
            LIMIT 1
        ) AS "PhotoUrl"
    FROM "Members" m
    WHERE m."FK_Families" = "p_FK_Families"
      AND m."IsCancelled" = FALSE
      AND (v_term IS NULL
           OR LOWER(m."FirstName") LIKE '%' || v_term || '%'
           OR LOWER(m."LastName") LIKE '%' || v_term || '%'
           OR LOWER(m."FirstName" || ' ' || m."LastName") LIKE '%' || v_term || '%')
      AND ("p_Gender" IS NULL OR "p_Gender" = '' OR m."Gender" = "p_Gender")
    ORDER BY
        CASE WHEN v_sort = 'lastname' AND NOT "p_SortDesc" THEN m."LastName" END ASC NULLS LAST,
        CASE WHEN v_sort = 'lastname' AND NOT "p_SortDesc" THEN m."FirstName" END ASC NULLS LAST,
        CASE WHEN v_sort = 'lastname' AND "p_SortDesc" THEN m."LastName" END DESC NULLS LAST,
        CASE WHEN v_sort = 'lastname' AND "p_SortDesc" THEN m."FirstName" END DESC NULLS LAST,
        CASE WHEN v_sort = 'dob' AND NOT "p_SortDesc" THEN m."DateOfBirth" END ASC NULLS LAST,
        CASE WHEN v_sort = 'dob' AND "p_SortDesc" THEN m."DateOfBirth" END DESC NULLS LAST,
        CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND NOT "p_SortDesc" THEN m."FirstName" END ASC NULLS LAST,
        CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND NOT "p_SortDesc" THEN m."LastName" END ASC NULLS LAST,
        CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND "p_SortDesc" THEN m."FirstName" END DESC NULLS LAST,
        CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND "p_SortDesc" THEN m."LastName" END DESC NULLS LAST
    OFFSET (v_page - 1) * v_size
    LIMIT v_size;
END;
$$;
