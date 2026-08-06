CREATE OR REPLACE FUNCTION "ProFamilyUpdate"(
    p_id           BIGINT,
    p_family_name  VARCHAR,
    p_description  VARCHAR,
    p_updated_by   VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
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
        RETURN QUERY SELECT 1, 404, 'Family not found.'::VARCHAR;
        RETURN;
    END IF;

    UPDATE "Families" f
    SET "FamilyName" = p_family_name,
        "Description" = p_description,
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE f."ID_Families" = p_id
      AND f."IsCancelled" = FALSE;

    RETURN QUERY SELECT 0, 200, 'Family updated successfully.'::VARCHAR;
END;
$$;
