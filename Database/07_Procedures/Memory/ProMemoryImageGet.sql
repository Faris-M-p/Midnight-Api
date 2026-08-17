DROP PROCEDURE IF EXISTS "ProMemoryImageGet";

CREATE OR REPLACE PROCEDURE "ProMemoryImageGet"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    "p_ID_MemoryImages" BIGINT,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT jsonb_build_object(
        'ImageId', img."ID_MemoryImages",
        'MemoryId', img."FK_Memories",
        'FileName', img."FileName",
        'StorageKey', img."StorageKey",
        'ImageUrl', img."ImageUrl",
        'FileUrl', img."ImageUrl",
        'FileSize', img."FileSize",
        'IsCoverImage', img."IsCoverImage"
    )
    INTO "p_Data"
    FROM "MemoryImages" img
    INNER JOIN "Memories" m
        ON m."ID_Memories" = img."FK_Memories"
       AND m."FK_Families" = "p_FK_Families"
       AND m."IsCancelled" = FALSE
    WHERE img."ID_MemoryImages" = "p_ID_MemoryImages"
      AND img."FK_Memories" = "p_ID_Memories"
      AND img."FK_Families" = "p_FK_Families"
      AND img."IsCancelled" = FALSE;

    IF "p_Data" IS NULL THEN
        "p_Data" := NULL;
    END IF;
END;
$$;
