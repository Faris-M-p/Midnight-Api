CREATE OR REPLACE FUNCTION "ProMemberHasRoot"(
    p_family_id         BIGINT,
    p_exclude_member_id BIGINT DEFAULT NULL
)
RETURNS BOOLEAN
LANGUAGE sql
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM "Members" m
        WHERE m."FK_Families" = p_family_id
          AND m."IsRoot" = TRUE
          AND m."IsCancelled" = FALSE
          AND (p_exclude_member_id IS NULL OR m."ID_Members" <> p_exclude_member_id)
    );
$$;
