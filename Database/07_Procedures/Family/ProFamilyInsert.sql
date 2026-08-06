CREATE OR REPLACE FUNCTION "ProFamilyInsert"(
    p_family_code  VARCHAR,
    p_family_name  VARCHAR,
    p_description  VARCHAR,
    p_created_by   VARCHAR
)
RETURNS TABLE (
    "ID_Families" BIGINT,
    "FamilyCode"  VARCHAR,
    "FamilyName"  VARCHAR,
    "Description" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id BIGINT;
BEGIN
    INSERT INTO "Families" ("FamilyCode", "FamilyName", "Description", "CreatedBy", "CreatedOn")
    VALUES (p_family_code, p_family_name, p_description, p_created_by, NOW())
    RETURNING "Families"."ID_Families" INTO v_id;

    RETURN QUERY
    SELECT f."ID_Families", f."FamilyCode", f."FamilyName", f."Description"
    FROM "Families" f
    WHERE f."ID_Families" = v_id;
END;
$$;
