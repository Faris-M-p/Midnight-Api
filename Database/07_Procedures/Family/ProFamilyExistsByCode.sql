CREATE OR REPLACE PROCEDURE "ProFamilyExistsByCode"(
    p_code              VARCHAR,
    p_exclude_family_id BIGINT DEFAULT NULL,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = p_code
          AND f."IsCancelled" = FALSE
          AND (p_exclude_family_id IS NULL OR f."ID_Families" <> p_exclude_family_id)
    )) INTO p_payload;
END;
$$;
