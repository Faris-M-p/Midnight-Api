CREATE OR REPLACE PROCEDURE "ProFamilyExistsByCode"(
    "p_FamilyCode" VARCHAR,
    "p_ExcludeFamilyId" BIGINT DEFAULT NULL,
    INOUT "p_Result" REFCURSOR DEFAULT 'family_exists_by_code'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = "p_FamilyCode"
          AND f."IsCancelled" = FALSE
          AND ("p_ExcludeFamilyId" IS NULL OR f."ID_Families" <> "p_ExcludeFamilyId")
    ) AS "Exists";
END;
$$;
