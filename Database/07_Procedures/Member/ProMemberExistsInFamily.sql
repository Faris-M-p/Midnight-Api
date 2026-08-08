CREATE OR REPLACE PROCEDURE "ProMemberExistsInFamily"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(EXISTS (
        SELECT 1
        FROM "Members" m
        WHERE m."ID_Members" = p_member_id
          AND m."FK_Families" = p_family_id
          AND m."IsCancelled" = FALSE
    )) INTO p_payload;
END;
$$;
