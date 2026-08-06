CREATE OR REPLACE FUNCTION "ProMemberTree"(
    p_family_id BIGINT
)
RETURNS TABLE (
    "Payload" JSONB
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_root_id BIGINT;
    v_total INTEGER;
    v_root JSONB;
BEGIN
    SELECT COUNT(*)::INTEGER INTO v_total
    FROM "Members" m
    WHERE m."FK_Families" = p_family_id
      AND m."IsCancelled" = FALSE;

    SELECT m."ID_Members" INTO v_root_id
    FROM "Members" m
    WHERE m."FK_Families" = p_family_id
      AND m."IsRoot" = TRUE
      AND m."IsCancelled" = FALSE
    LIMIT 1;

    IF v_root_id IS NULL THEN
        RETURN QUERY SELECT jsonb_build_object(
            'Root', NULL,
            'TotalMembers', v_total
        );
        RETURN;
    END IF;

    v_root := "FnBuildTreeNode"(v_root_id, TRUE);

    RETURN QUERY SELECT jsonb_build_object(
        'Root', v_root,
        'TotalMembers', v_total
    );
END;
$$;
