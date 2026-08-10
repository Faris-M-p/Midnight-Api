CREATE OR REPLACE PROCEDURE "ProMemberIsInScopeBranch"(
    "p_FK_Families" BIGINT,
    "p_RootMemberId" BIGINT,
    "p_CandidateMemberId" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'member_in_scope_branch'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    WITH RECURSIVE branch AS (
        SELECT m."ID_Members"
        FROM "Members" m
        WHERE m."ID_Members" = "p_RootMemberId"
          AND m."FK_Families" = "p_FK_Families"
          AND m."IsCancelled" = FALSE

        UNION ALL

        SELECT c."ID_Members"
        FROM "Members" c
        INNER JOIN branch b ON c."FK_Members_Parent" = b."ID_Members"
        WHERE c."FK_Families" = "p_FK_Families"
          AND c."IsCancelled" = FALSE
    )
    SELECT EXISTS (
        SELECT 1
        FROM branch
        WHERE "ID_Members" = "p_CandidateMemberId"
    ) AS "IsInBranch";
END;
$$;
