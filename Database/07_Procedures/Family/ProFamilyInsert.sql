CREATE OR REPLACE PROCEDURE "ProFamilyInsert"(
    p_family_code  VARCHAR,
    p_family_name  VARCHAR,
    p_description  VARCHAR,
    p_created_by   VARCHAR,
    INOUT p_response_code INTEGER DEFAULT 0,
    INOUT p_status_code INTEGER DEFAULT 0,
    INOUT p_response_message VARCHAR DEFAULT NULL
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
        p_response_code := 1;
        p_status_code := 409;
        p_response_message := 'Family code already exists.';
        RETURN;
    END IF;

    INSERT INTO "Families" ("FamilyCode", "FamilyName", "Description", "CreatedBy", "CreatedOn")
    VALUES (p_family_code, p_family_name, p_description, p_created_by, NOW());

    p_response_code := 0;
    p_status_code := 201;
    p_response_message := 'Family created successfully.';
EXCEPTION WHEN OTHERS THEN
    p_response_code := 99;
    p_status_code := 500;
    p_response_message := SQLERRM;
END;
$$;
