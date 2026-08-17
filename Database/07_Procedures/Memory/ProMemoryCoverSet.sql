DROP PROCEDURE IF EXISTS "ProMemoryCoverSet"(BIGINT, BIGINT, BIGINT, VARCHAR, BIGINT, BOOLEAN, VARCHAR, JSONB);
DROP PROCEDURE IF EXISTS "ProMemoryCoverSet";

CREATE OR REPLACE PROCEDURE "ProMemoryCoverSet"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    "p_ID_MemoryImages" BIGINT,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_image_url VARCHAR(2000);
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Memories"
        WHERE "ID_Memories" = "p_ID_Memories"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Memory not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    SELECT img."ImageUrl"
    INTO v_image_url
    FROM "MemoryImages" img
    WHERE img."ID_MemoryImages" = "p_ID_MemoryImages"
      AND img."FK_Memories" = "p_ID_Memories"
      AND img."FK_Families" = "p_FK_Families"
      AND img."IsCancelled" = FALSE;

    IF NOT FOUND THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Image not found on this memory.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "MemoryImages"
    SET "IsCoverImage" = FALSE,
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "FK_Memories" = "p_ID_Memories"
      AND "IsCancelled" = FALSE
      AND "IsCoverImage" = TRUE
      AND "ID_MemoryImages" IS DISTINCT FROM "p_ID_MemoryImages";

    UPDATE "MemoryImages"
    SET "IsCoverImage" = TRUE,
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_MemoryImages" = "p_ID_MemoryImages"
      AND "FK_Memories" = "p_ID_Memories"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    UPDATE "Memories"
    SET "CoverImageUrl" = v_image_url,
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Memories" = "p_ID_Memories"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_MemoryImages";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Cover image updated successfully.';
    "p_Data" := jsonb_build_object(
        'ImageId', "p_ID_MemoryImages",
        'MemoryId', "p_ID_Memories"
    );
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
