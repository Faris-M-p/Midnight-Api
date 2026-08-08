CREATE OR REPLACE PROCEDURE "ProMemberGetRelation"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(t) INTO p_payload
    FROM (
        SELECT m."FK_Members_Parent" AS "ParentId", m."FK_Members_Spouse" AS "SpouseId"
        FROM "Members" m
        WHERE m."ID_Members" = p_member_id
          AND m."FK_Families" = p_family_id
          AND m."IsCancelled" = FALSE
    ) t;
END;
$$;
