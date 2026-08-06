CREATE OR REPLACE FUNCTION "ProFamilyInsert"(
    p_family_code  VARCHAR,
    p_family_name  VARCHAR,
    p_description  VARCHAR,
    p_created_by   VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = p_family_code
          AND f."IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 1, 409, 'Family code already exists.'::VARCHAR;
        RETURN;
    END IF;

    INSERT INTO "Families" ("FamilyCode", "FamilyName", "Description", "CreatedBy", "CreatedOn")
    VALUES (p_family_code, p_family_name, p_description, p_created_by, NOW());

    RETURN QUERY SELECT 0, 201, 'Family created successfully.'::VARCHAR;
END;
$$;
