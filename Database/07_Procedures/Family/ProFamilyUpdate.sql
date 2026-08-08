CREATE OR REPLACE PROCEDURE "ProFamilyUpdate"(
    p_id           BIGINT,
    p_family_name  VARCHAR,
    p_description  VARCHAR,
    p_updated_by   VARCHAR,
    INOUT p_response_code INTEGER DEFAULT 0,
    INOUT p_status_code INTEGER DEFAULT 0,
    INOUT p_response_message VARCHAR DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."ID_Families" = p_id
          AND f."IsCancelled" = FALSE
    ) THEN
        p_response_code := 1;
        p_status_code := 404;
        p_response_message := 'Family not found.';
        RETURN;
    END IF;

    UPDATE "Families" f
    SET "FamilyName" = p_family_name,
        "Description" = p_description,
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE f."ID_Families" = p_id
      AND f."IsCancelled" = FALSE;

    p_response_code := 0;
    p_status_code := 200;
    p_response_message := 'Family updated successfully.';
EXCEPTION WHEN OTHERS THEN
    p_response_code := 99;
    p_status_code := 500;
    p_response_message := SQLERRM;
END;
$$;
