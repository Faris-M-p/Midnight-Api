CREATE OR REPLACE FUNCTION "FnBuildTreeNode"(
    p_member_id       BIGINT,
    p_include_children BOOLEAN
)
RETURNS JSONB
LANGUAGE plpgsql
AS $$
DECLARE
    v_row RECORD;
    v_spouse JSONB;
    v_children JSONB;
BEGIN
    SELECT
        m."ID_Members" AS id,
        m."FirstName" AS first_name,
        m."LastName" AS last_name,
        (m."FirstName" || ' ' || m."LastName") AS full_name,
        m."Gender" AS gender,
        m."DateOfBirth" AS dob,
        m."DateOfDeath" AS dod,
        m."IsRoot" AS is_root,
        m."Nickname" AS nickname,
        m."Profession" AS profession,
        m."Biography" AS biography,
        m."FK_Members_Spouse" AS spouse_id,
        (
            SELECT i."ImageUrl"
            FROM "MemberImages" i
            WHERE i."FK_Members" = m."ID_Members"
              AND i."IsCancelled" = FALSE
            ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
            LIMIT 1
        ) AS photo_url
    INTO v_row
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id
      AND m."IsCancelled" = FALSE;

    IF NOT FOUND THEN
        RETURN NULL;
    END IF;

    v_spouse := NULL;
    IF v_row.spouse_id IS NOT NULL THEN
        v_spouse := "FnBuildTreeNode"(v_row.spouse_id, FALSE);
    END IF;

    v_children := '[]'::jsonb;
    IF p_include_children THEN
        SELECT COALESCE(jsonb_agg("FnBuildTreeNode"(c."ID_Members", TRUE) ORDER BY c."ID_Members"), '[]'::jsonb)
        INTO v_children
        FROM "Members" c
        WHERE c."FK_Members_Parent" = p_member_id
          AND c."IsCancelled" = FALSE;
    END IF;

    RETURN jsonb_build_object(
        'Id', v_row.id,
        'FirstName', v_row.first_name,
        'LastName', v_row.last_name,
        'FullName', v_row.full_name,
        'Gender', v_row.gender,
        'DateOfBirth', v_row.dob,
        'DateOfDeath', v_row.dod,
        'IsRoot', v_row.is_root,
        'Nickname', v_row.nickname,
        'Profession', v_row.profession,
        'Biography', v_row.biography,
        'PhotoUrl', v_row.photo_url,
        'Spouse', v_spouse,
        'Children', v_children
    );
END;
$$;
