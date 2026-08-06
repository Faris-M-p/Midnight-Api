CREATE OR REPLACE FUNCTION "ProMemberList"(
    p_family_id BIGINT,
    p_search    VARCHAR DEFAULT NULL,
    p_gender    VARCHAR DEFAULT NULL,
    p_sort_by   VARCHAR DEFAULT 'firstname',
    p_sort_desc BOOLEAN DEFAULT FALSE,
    p_page      INTEGER DEFAULT 1,
    p_page_size INTEGER DEFAULT 20
)
RETURNS TABLE (
    "Id"           BIGINT,
    "FirstName"    VARCHAR,
    "LastName"     VARCHAR,
    "FullName"     TEXT,
    "Gender"       VARCHAR,
    "DateOfBirth"  DATE,
    "IsRoot"       BOOLEAN,
    "Profession"   VARCHAR,
    "PhotoUrl"     VARCHAR,
    "TotalCount"   BIGINT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_page INTEGER := GREATEST(COALESCE(p_page, 1), 1);
    v_size INTEGER := LEAST(GREATEST(COALESCE(p_page_size, 20), 1), 100);
    v_term TEXT := NULLIF(LOWER(TRIM(COALESCE(p_search, ''))), '');
    v_sort TEXT := LOWER(COALESCE(p_sort_by, 'firstname'));
BEGIN
    RETURN QUERY
    WITH filtered AS (
        SELECT
            m."ID_Members",
            m."FirstName",
            m."LastName",
            m."Gender",
            m."DateOfBirth",
            m."IsRoot",
            m."Profession",
            (
                SELECT i."ImageUrl"
                FROM "MemberImages" i
                WHERE i."FK_Members" = m."ID_Members"
                  AND i."IsCancelled" = FALSE
                ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
                LIMIT 1
            ) AS photo
        FROM "Members" m
        WHERE m."FK_Families" = p_family_id
          AND m."IsCancelled" = FALSE
          AND (v_term IS NULL
               OR LOWER(m."FirstName") LIKE '%' || v_term || '%'
               OR LOWER(m."LastName") LIKE '%' || v_term || '%'
               OR LOWER(m."FirstName" || ' ' || m."LastName") LIKE '%' || v_term || '%')
          AND (p_gender IS NULL OR p_gender = '' OR m."Gender" = p_gender)
    ),
    counted AS (
        SELECT COUNT(*)::BIGINT AS total FROM filtered
    ),
    ordered AS (
        SELECT f.*
        FROM filtered f
        ORDER BY
            CASE WHEN v_sort = 'lastname' AND NOT p_sort_desc THEN f."LastName" END ASC NULLS LAST,
            CASE WHEN v_sort = 'lastname' AND NOT p_sort_desc THEN f."FirstName" END ASC NULLS LAST,
            CASE WHEN v_sort = 'lastname' AND p_sort_desc THEN f."LastName" END DESC NULLS LAST,
            CASE WHEN v_sort = 'lastname' AND p_sort_desc THEN f."FirstName" END DESC NULLS LAST,
            CASE WHEN v_sort = 'dob' AND NOT p_sort_desc THEN f."DateOfBirth" END ASC NULLS LAST,
            CASE WHEN v_sort = 'dob' AND p_sort_desc THEN f."DateOfBirth" END DESC NULLS LAST,
            CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND NOT p_sort_desc THEN f."FirstName" END ASC NULLS LAST,
            CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND NOT p_sort_desc THEN f."LastName" END ASC NULLS LAST,
            CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND p_sort_desc THEN f."FirstName" END DESC NULLS LAST,
            CASE WHEN v_sort <> 'lastname' AND v_sort <> 'dob' AND p_sort_desc THEN f."LastName" END DESC NULLS LAST
        OFFSET (v_page - 1) * v_size
        LIMIT v_size
    )
    SELECT
        o."ID_Members",
        o."FirstName",
        o."LastName",
        (o."FirstName" || ' ' || o."LastName"),
        o."Gender",
        o."DateOfBirth",
        o."IsRoot",
        o."Profession",
        o.photo,
        c.total
    FROM ordered o
    CROSS JOIN counted c;
END;
$$;
