CREATE OR REPLACE FUNCTION "ProMemberExistsInFamily"(
    p_family_id BIGINT,
    p_member_id BIGINT
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM "Members" m
        WHERE m."ID_Members" = p_member_id
          AND m."FK_Families" = p_family_id
          AND m."IsCancelled" = FALSE
    );
$$;
