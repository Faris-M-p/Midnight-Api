CREATE OR REPLACE FUNCTION "ProMemberGetRelation"(
    p_family_id BIGINT,
    p_member_id BIGINT
)
RETURNS TABLE (
    "ParentId" BIGINT,
    "SpouseId" BIGINT
)
LANGUAGE sql
AS $$
    SELECT m."FK_Members_Parent", m."FK_Members_Spouse"
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id
      AND m."FK_Families" = p_family_id
      AND m."IsCancelled" = FALSE;
$$;
