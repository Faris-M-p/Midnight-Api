CREATE OR REPLACE PROCEDURE "ProMemberGetParentId"(
    p_member_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT to_jsonb(m."FK_Members_Parent")
    INTO p_payload
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id
      AND m."IsCancelled" = FALSE;
END;
$$;
