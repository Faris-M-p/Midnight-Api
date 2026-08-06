CREATE OR REPLACE FUNCTION "FnGetRelationship"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    p_other_id BIGINT
)
RETURNS VARCHAR(50)
LANGUAGE plpgsql
AS $$
DECLARE
    v_parent BIGINT;
    v_spouse BIGINT;
    v_other_parent BIGINT;
BEGIN
    IF p_member_id = p_other_id THEN
        RETURN 'Self';
    END IF;

    SELECT m."FK_Members_Parent", m."FK_Members_Spouse"
    INTO v_parent, v_spouse
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id
      AND m."FK_Families" = p_family_id
      AND m."IsCancelled" = FALSE;

    IF v_spouse = p_other_id THEN
        RETURN 'Spouse';
    END IF;

    IF v_parent = p_other_id THEN
        RETURN 'Parent';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM "Members" c
        WHERE c."ID_Members" = p_other_id
          AND c."FK_Members_Parent" = p_member_id
          AND c."IsCancelled" = FALSE
    ) THEN
        RETURN 'Child';
    END IF;

    SELECT o."FK_Members_Parent"
    INTO v_other_parent
    FROM "Members" o
    WHERE o."ID_Members" = p_other_id
      AND o."FK_Families" = p_family_id
      AND o."IsCancelled" = FALSE;

    IF v_parent IS NOT NULL AND v_parent = v_other_parent THEN
        RETURN 'Sibling';
    END IF;

    RETURN 'Related';
END;
$$;
