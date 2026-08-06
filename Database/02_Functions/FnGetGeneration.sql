CREATE OR REPLACE FUNCTION "FnGetGeneration"(
    p_family_id BIGINT,
    p_member_id BIGINT
)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_depth INTEGER := 0;
    v_current BIGINT := p_member_id;
    v_parent BIGINT;
BEGIN
    IF p_member_id IS NULL THEN
        RETURN 0;
    END IF;

    LOOP
        v_depth := v_depth + 1;

        SELECT m."FK_Members_Parent"
        INTO v_parent
        FROM "Members" m
        WHERE m."ID_Members" = v_current
          AND m."FK_Families" = p_family_id
          AND m."IsCancelled" = FALSE;

        EXIT WHEN v_parent IS NULL;
        v_current := v_parent;

        IF v_depth > 1000 THEN
            RAISE EXCEPTION 'Generation depth limit exceeded';
        END IF;
    END LOOP;

    RETURN v_depth;
END;
$$;
