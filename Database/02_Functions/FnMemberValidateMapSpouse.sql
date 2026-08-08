DROP FUNCTION IF EXISTS "FnMemberValidateMapSpouse"(BIGINT, BIGINT, BIGINT);

CREATE OR REPLACE FUNCTION "FnMemberValidateMapSpouse"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
    "p_FK_Members_Spouse" BIGINT
)
RETURNS TABLE (
    "ResponseCode" INTEGER,
    "Status" BOOLEAN,
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
    IF "p_ID_Members" = "p_FK_Members_Spouse" THEN
        RETURN QUERY SELECT 20, FALSE, 'A member cannot marry themselves.'::VARCHAR;
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = "p_ID_Members"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) OR NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = "p_FK_Members_Spouse"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 30, FALSE, 'One or both members were not found in this family.'::VARCHAR;
        RETURN;
    END IF;

    SELECT m."FK_Members_Parent", m."FK_Members_Spouse"
    INTO v_left_parent, v_left_spouse
    FROM "Members" m
    WHERE m."ID_Members" = "p_ID_Members";

    SELECT m."FK_Members_Parent", m."FK_Members_Spouse"
    INTO v_right_parent, v_right_spouse
    FROM "Members" m
    WHERE m."ID_Members" = "p_FK_Members_Spouse";

    IF v_left_spouse IS NOT NULL OR v_right_spouse IS NOT NULL THEN
        RETURN QUERY SELECT 20, FALSE, 'One or both members already have a spouse.'::VARCHAR;
        RETURN;
    END IF;

    IF v_left_parent = "p_FK_Members_Spouse" OR v_right_parent = "p_ID_Members" THEN
        RETURN QUERY SELECT 20, FALSE, 'Parent-child relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    IF v_left_parent IS NOT NULL
       AND v_right_parent IS NOT NULL
       AND v_left_parent = v_right_parent THEN
        RETURN QUERY SELECT 20, FALSE, 'Sibling relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    WITH RECURSIVE climb AS (
        SELECT "p_FK_Members_Spouse" AS id
        UNION ALL
        SELECT m."FK_Members_Parent"
        FROM "Members" m
        INNER JOIN climb c ON m."ID_Members" = c.id
        WHERE m."FK_Members_Parent" IS NOT NULL
          AND m."IsCancelled" = FALSE
    )
    SELECT c.id INTO v_hit
    FROM climb c
    WHERE c.id = "p_ID_Members"
    LIMIT 1;

    IF v_hit IS NOT NULL THEN
        RETURN QUERY SELECT 20, FALSE, 'Ancestor-descendant relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    v_hit := NULL;
    WITH RECURSIVE climb AS (
        SELECT "p_ID_Members" AS id
        UNION ALL
        SELECT m."FK_Members_Parent"
        FROM "Members" m
        INNER JOIN climb c ON m."ID_Members" = c.id
        WHERE m."FK_Members_Parent" IS NOT NULL
          AND m."IsCancelled" = FALSE
    )
    SELECT c.id INTO v_hit
    FROM climb c
    WHERE c.id = "p_FK_Members_Spouse"
    LIMIT 1;

    IF v_hit IS NOT NULL THEN
        RETURN QUERY SELECT 20, FALSE, 'Ancestor-descendant relationship cannot be mapped as spouses.'::VARCHAR;
        RETURN;
    END IF;

    RETURN QUERY SELECT 10, TRUE, 'OK'::VARCHAR;
END;
$$;
