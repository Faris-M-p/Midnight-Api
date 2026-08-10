CREATE OR REPLACE PROCEDURE "ProFamilySelectByCode"(
    "p_FamilyCode" VARCHAR,
    INOUT "p_Result" REFCURSOR DEFAULT 'family_by_code'
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_prefix TEXT := upper(regexp_replace(COALESCE("p_FamilyCode", ''), '[^A-Za-z0-9]', '', 'g'));
BEGIN
    OPEN "p_Result" FOR
    SELECT
        f."ID_Families" AS "Id",
        f."FamilyCode" AS "FamilyCode",
        f."FamilyName" AS "FamilyName"
    FROM "Families" f
    WHERE f."IsCancelled" = FALSE
      AND (
            f."FamilyCode" = "p_FamilyCode"
         OR upper(regexp_replace(f."FamilyCode", '[^A-Za-z0-9]', '', 'g')) = v_prefix
      )
    ORDER BY
        CASE WHEN f."FamilyCode" = "p_FamilyCode" THEN 0 ELSE 1 END,
        f."ID_Families"
    LIMIT 1;
END;
$$;
