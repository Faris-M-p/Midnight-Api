CREATE OR REPLACE PROCEDURE "ProFamilySelect"(
    p_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(t) INTO p_payload
    FROM (
        SELECT f."ID_Families", f."FamilyCode", f."FamilyName", f."Description"
        FROM "Families" f
        WHERE f."ID_Families" = p_id
          AND f."IsCancelled" = FALSE
    ) t;
END;
$$;
