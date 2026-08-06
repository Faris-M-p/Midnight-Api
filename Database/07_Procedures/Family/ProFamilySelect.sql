CREATE OR REPLACE FUNCTION "ProFamilySelect"(
    p_id BIGINT
)
RETURNS TABLE (
    "ID_Families" BIGINT,
    "FamilyCode"  VARCHAR,
    "FamilyName"  VARCHAR,
    "Description" VARCHAR
)
LANGUAGE sql
AS $$
    SELECT f."ID_Families", f."FamilyCode", f."FamilyName", f."Description"
    FROM "Families" f
    WHERE f."ID_Families" = p_id
      AND f."IsCancelled" = FALSE;
$$;
