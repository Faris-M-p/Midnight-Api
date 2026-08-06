CREATE OR REPLACE FUNCTION "FnMemberValidateMapSpouse"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    p_spouse_id BIGINT
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_left_parent  BIGINT;
    v_left_spouse  BIGINT;
    v_right_parent BIGINT;
    v_right_spouse BIGINT;
    v_hit          BIGINT;
BEGIN
    IF p_member_id = p_spouse_id THEN
        RETURN QUERY SELECT 1, 400, 'A member cannot marry themselves.'::VARCHAR;
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = p_member_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) OR NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = p_spouse_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 2, 404, 'One or both members were not found in this family.'::VARCHAR;
        RETURN;
    END IF;

    SELECT m."FK_Members_Parent", m."FK_Members_Spouse"
    INTO v_left_parent, v_left_spouse
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id;

    SELECT m."FK_Members_Parent", m."FK_Members_Spouse"
    INTO v_right_parent, v_right_spouse
    FROM "Members" m
    WHERE m."ID_Members" = p_spouse_id;

    IF v_left_spouse IS NOT NULL OR v_right_spouse IS NOT NULL THEN
        RETURN QUERY SELECT 3, 400, 'One or both members already have a spouse.'::VARCHAR;
        RETURN;
    END IF;

    IF v_left_parent = p_spouse_id OR v_right_parent = p_member_id THEN
        RETURN QUERY SELECT 4, 400, 'Parent-child relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    IF v_left_parent IS NOT NULL
       AND v_right_parent IS NOT NULL
       AND v_left_parent = v_right_parent THEN
        RETURN QUERY SELECT 5, 400, 'Sibling relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    WITH RECURSIVE climb AS (
        SELECT p_spouse_id AS id
        UNION ALL
        SELECT m."FK_Members_Parent"
        FROM "Members" m
        INNER JOIN climb c ON m."ID_Members" = c.id
        WHERE m."FK_Members_Parent" IS NOT NULL
          AND m."IsCancelled" = FALSE
    )
    SELECT c.id INTO v_hit
    FROM climb c
    WHERE c.id = p_member_id
    LIMIT 1;

    IF v_hit IS NOT NULL THEN
        RETURN QUERY SELECT 6, 400, 'Ancestor-descendant relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    v_hit := NULL;
    WITH RECURSIVE climb AS (
        SELECT p_member_id AS id
        UNION ALL
        SELECT m."FK_Members_Parent"
        FROM "Members" m
        INNER JOIN climb c ON m."ID_Members" = c.id
        WHERE m."FK_Members_Parent" IS NOT NULL
          AND m."IsCancelled" = FALSE
    )
    SELECT c.id INTO v_hit
    FROM climb c
    WHERE c.id = p_spouse_id
    LIMIT 1;

    IF v_hit IS NOT NULL THEN
        RETURN QUERY SELECT 6, 400, 'Ancestor-descendant relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    RETURN QUERY SELECT 0, 200, 'OK'::VARCHAR;
END;
$$;
