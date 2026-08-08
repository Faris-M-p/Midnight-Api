CREATE OR REPLACE PROCEDURE "ProMemberHasRoot"(
    "p_FK_Families" BIGINT,
    "p_ExcludeMemberId" BIGINT DEFAULT NULL,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_has_root'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT EXISTS (
        SELECT 1
        FROM "Members" m
        WHERE m."FK_Families" = "p_FK_Families"
          AND m."IsRoot" = TRUE
          AND m."IsCancelled" = FALSE
          AND ("p_ExcludeMemberId" IS NULL OR m."ID_Members" <> "p_ExcludeMemberId")
    ) AS "Exists";
END;
$$;
