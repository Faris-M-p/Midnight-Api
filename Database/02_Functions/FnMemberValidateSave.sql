CREATE OR REPLACE FUNCTION "FnMemberValidateSave"(
    p_family_id  BIGINT,
    p_member_id  BIGINT,
    p_parent_id  BIGINT,
    p_spouse_id  BIGINT,
    p_is_root    BOOLEAN
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_hit BIGINT;
BEGIN
    IF p_member_id IS NOT NULL AND p_member_id > 0 THEN
        IF NOT EXISTS (
            SELECT 1 FROM "Members"
            WHERE "ID_Members" = p_member_id
              AND "FK_Families" = p_family_id
              AND "IsCancelled" = FALSE
        ) THEN
            RETURN QUERY SELECT 1, 404, 'Member not found.'::VARCHAR;
            RETURN;
        END IF;
    END IF;

    IF p_parent_id IS NOT NULL AND p_member_id IS NOT NULL AND p_parent_id = p_member_id THEN
        RETURN QUERY SELECT 2, 400, 'A member cannot be their own parent.'::VARCHAR;
        RETURN;
    END IF;

    IF p_spouse_id IS NOT NULL AND p_member_id IS NOT NULL AND p_spouse_id = p_member_id THEN
        RETURN QUERY SELECT 3, 400, 'A member cannot marry themselves.'::VARCHAR;
        RETURN;
    END IF;

    IF COALESCE(p_is_root, FALSE)
       AND EXISTS (
           SELECT 1 FROM "Members" m
           WHERE m."FK_Families" = p_family_id
             AND m."IsRoot" = TRUE
             AND m."IsCancelled" = FALSE
             AND (p_member_id IS NULL OR m."ID_Members" <> p_member_id)
       ) THEN
        RETURN QUERY SELECT 4, 400, 'Only one root member is allowed per family.'::VARCHAR;
        RETURN;
    END IF;

    IF p_parent_id IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = p_parent_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 5, 404, 'Parent member not found.'::VARCHAR;
        RETURN;
    END IF;

    IF p_spouse_id IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = p_spouse_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 6, 404, 'Spouse member not found.'::VARCHAR;
        RETURN;
    END IF;

    IF p_parent_id IS NOT NULL THEN
        WITH RECURSIVE climb AS (
            SELECT p_parent_id AS id
            UNION ALL
            SELECT m."FK_Members_Parent"
            FROM "Members" m
            INNER JOIN climb c ON m."ID_Members" = c.id
            WHERE m."FK_Members_Parent" IS NOT NULL
              AND m."IsCancelled" = FALSE
        )
        SELECT c.id INTO v_hit
        FROM climb c
        WHERE p_member_id IS NOT NULL
          AND p_member_id > 0
          AND c.id = p_member_id
        LIMIT 1;

        IF v_hit IS NOT NULL THEN
            RETURN QUERY SELECT 7, 400, 'Circular parent reference detected.'::VARCHAR;
            RETURN;
        END IF;
    END IF;

    RETURN QUERY SELECT 0, 200, 'OK'::VARCHAR;
END;
$$;
