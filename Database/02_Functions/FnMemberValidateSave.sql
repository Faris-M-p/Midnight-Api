DROP FUNCTION IF EXISTS "FnMemberValidateSave"(BIGINT, BIGINT, BIGINT, BIGINT, BOOLEAN);

CREATE OR REPLACE FUNCTION "FnMemberValidateSave"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
    "p_FK_Members_Parent" BIGINT,
    "p_FK_Members_Spouse" BIGINT,
    "p_IsRoot" BOOLEAN
)
RETURNS TABLE (
    "ResponseCode" INTEGER,
    "Status" BOOLEAN,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_hit BIGINT;
BEGIN
    IF "p_ID_Members" IS NOT NULL AND "p_ID_Members" > 0 THEN
        IF NOT EXISTS (
            SELECT 1 FROM "Members"
            WHERE "ID_Members" = "p_ID_Members"
              AND "FK_Families" = "p_FK_Families"
              AND "IsCancelled" = FALSE
        ) THEN
            RETURN QUERY SELECT 30, FALSE, 'Member not found.'::VARCHAR;
            RETURN;
        END IF;
    END IF;

    IF "p_FK_Members_Parent" IS NOT NULL AND "p_ID_Members" IS NOT NULL AND "p_FK_Members_Parent" = "p_ID_Members" THEN
        RETURN QUERY SELECT 20, FALSE, 'A member cannot be their own parent.'::VARCHAR;
        RETURN;
    END IF;

    IF "p_FK_Members_Spouse" IS NOT NULL AND "p_ID_Members" IS NOT NULL AND "p_FK_Members_Spouse" = "p_ID_Members" THEN
        RETURN QUERY SELECT 20, FALSE, 'A member cannot marry themselves.'::VARCHAR;
        RETURN;
    END IF;

    IF COALESCE("p_IsRoot", FALSE)
       AND EXISTS (
           SELECT 1 FROM "Members" m
           WHERE m."FK_Families" = "p_FK_Families"
             AND m."IsRoot" = TRUE
             AND m."IsCancelled" = FALSE
             AND ("p_ID_Members" IS NULL OR m."ID_Members" <> "p_ID_Members")
       ) THEN
        RETURN QUERY SELECT 20, FALSE, 'Only one root member is allowed per family.'::VARCHAR;
        RETURN;
    END IF;

    IF "p_FK_Members_Parent" IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = "p_FK_Members_Parent"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 30, FALSE, 'Parent member not found.'::VARCHAR;
        RETURN;
    END IF;

    IF "p_FK_Members_Spouse" IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = "p_FK_Members_Spouse"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 30, FALSE, 'Spouse member not found.'::VARCHAR;
        RETURN;
    END IF;

    IF "p_FK_Members_Parent" IS NOT NULL THEN
        WITH RECURSIVE climb AS (
            SELECT "p_FK_Members_Parent" AS id
            UNION ALL
            SELECT m."FK_Members_Parent"
            FROM "Members" m
            INNER JOIN climb c ON m."ID_Members" = c.id
            WHERE m."FK_Members_Parent" IS NOT NULL
              AND m."IsCancelled" = FALSE
        )
        SELECT c.id INTO v_hit
        FROM climb c
        WHERE "p_ID_Members" IS NOT NULL
          AND "p_ID_Members" > 0
          AND c.id = "p_ID_Members"
        LIMIT 1;

        IF v_hit IS NOT NULL THEN
            RETURN QUERY SELECT 20, FALSE, 'Circular parent reference detected.'::VARCHAR;
            RETURN;
        END IF;
    END IF;

    RETURN QUERY SELECT 10, TRUE, 'OK'::VARCHAR;
END;
$$;
