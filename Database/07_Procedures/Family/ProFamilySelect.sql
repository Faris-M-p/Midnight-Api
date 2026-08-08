CREATE OR REPLACE PROCEDURE "ProFamilySelect"(
    "p_ID_Families" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'family_select'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT f."ID_Families", f."FamilyCode", f."FamilyName", f."Description"
    FROM "Families" f
    WHERE f."ID_Families" = "p_ID_Families"
      AND f."IsCancelled" = FALSE;
END;
$$;
