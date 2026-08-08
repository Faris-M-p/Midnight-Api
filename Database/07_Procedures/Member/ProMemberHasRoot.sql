CREATE OR REPLACE PROCEDURE "ProMemberHasRoot"(
    p_family_id         BIGINT,
    p_exclude_member_id BIGINT DEFAULT NULL,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(EXISTS (
        SELECT 1
        FROM "Members" m
        WHERE m."FK_Families" = p_family_id
          AND m."IsRoot" = TRUE
          AND m."IsCancelled" = FALSE
          AND (p_exclude_member_id IS NULL OR m."ID_Members" <> p_exclude_member_id)
    )) INTO p_payload;
END;
$$;
