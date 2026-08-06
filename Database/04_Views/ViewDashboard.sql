CREATE OR REPLACE VIEW "ViewDashboard" AS
SELECT
    f."ID_Families",
    f."FamilyCode",
    f."FamilyName",
    f."Description",
    COUNT(m."ID_Members") FILTER (WHERE m."ID_Members" IS NOT NULL) AS "TotalMembers",
    COUNT(m."ID_Members") FILTER (WHERE LOWER(COALESCE(m."Gender", '')) = 'male') AS "MaleCount",
    COUNT(m."ID_Members") FILTER (WHERE LOWER(COALESCE(m."Gender", '')) = 'female') AS "FemaleCount",
    COUNT(m."ID_Members") FILTER (
        WHERE m."ID_Members" IS NOT NULL
          AND LOWER(COALESCE(m."Gender", '')) NOT IN ('male', 'female')
    ) AS "OtherGenderCount",
    COUNT(m."ID_Members") FILTER (WHERE m."DateOfDeath" IS NULL) AS "LivingCount",
    COUNT(m."ID_Members") FILTER (WHERE m."DateOfDeath" IS NOT NULL) AS "DeceasedCount"
FROM "Families" f
LEFT JOIN "Members" m
    ON m."FK_Families" = f."ID_Families"
   AND m."IsCancelled" = FALSE
WHERE f."IsCancelled" = FALSE
GROUP BY f."ID_Families", f."FamilyCode", f."FamilyName", f."Description";
