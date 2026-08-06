CREATE OR REPLACE FUNCTION "ProMemberGetParentId"(
    p_member_id BIGINT
)
RETURNS BIGINT
LANGUAGE sql
AS $$
    SELECT m."FK_Members_Parent"
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id
      AND m."IsCancelled" = FALSE;
$$;
