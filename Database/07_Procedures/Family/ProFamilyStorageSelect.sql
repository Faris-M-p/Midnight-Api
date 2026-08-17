CREATE OR REPLACE PROCEDURE "ProFamilyStorageSelect"(
    "p_FK_Families" BIGINT,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_member_bytes BIGINT := 0;
    v_memory_bytes BIGINT := 0;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Families"
        WHERE "ID_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_Data" := NULL;
        RETURN;
    END IF;

    SELECT COALESCE(SUM(mi."FileSize"), 0)
    INTO v_member_bytes
    FROM "MemberImages" mi
    INNER JOIN "Members" m
        ON m."ID_Members" = mi."FK_Members"
    WHERE m."FK_Families" = "p_FK_Families"
      AND mi."IsCancelled" = FALSE
      AND m."IsCancelled" = FALSE;

    SELECT COALESCE(SUM(img."FileSize"), 0)
    INTO v_memory_bytes
    FROM "MemoryImages" img
    WHERE img."FK_Families" = "p_FK_Families"
      AND img."IsCancelled" = FALSE;

    "p_Data" := jsonb_build_object(
        'FamilyId', "p_FK_Families",
        'MemberImagesStorageBytes', v_member_bytes,
        'MemoryImagesStorageBytes', v_memory_bytes,
        'StorageUsedBytes', v_member_bytes + v_memory_bytes
    );
END;
$$;
