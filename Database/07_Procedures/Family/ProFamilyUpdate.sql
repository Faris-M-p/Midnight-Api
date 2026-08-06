CREATE OR REPLACE FUNCTION "ProFamilyUpdate"(
    p_id           BIGINT,
    p_family_name  VARCHAR,
    p_description  VARCHAR,
    p_updated_by   VARCHAR
)
RETURNS TABLE (
    "ID_Families" BIGINT,
    "FamilyCode"  VARCHAR,
    "FamilyName"  VARCHAR,
    "Description" VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "Families" f
    SET "FamilyName" = p_family_name,
        "Description" = p_description,
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE f."ID_Families" = p_id
      AND f."IsCancelled" = FALSE;

    RETURN QUERY
    SELECT f."ID_Families", f."FamilyCode", f."FamilyName", f."Description"
    FROM "Families" f
    WHERE f."ID_Families" = p_id
      AND f."IsCancelled" = FALSE;
END;
$$;
