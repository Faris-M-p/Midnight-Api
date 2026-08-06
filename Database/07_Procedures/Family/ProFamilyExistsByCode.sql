CREATE OR REPLACE FUNCTION "ProFamilyExistsByCode"(
    p_code              VARCHAR,
    p_exclude_family_id BIGINT DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = p_code
          AND f."IsCancelled" = FALSE
          AND (p_exclude_family_id IS NULL OR f."ID_Families" <> p_exclude_family_id)
    );
$$;
