DROP PROCEDURE IF EXISTS "ProMemoryImageDelete";

CREATE OR REPLACE PROCEDURE "ProMemoryImageDelete"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    "p_ID_MemoryImages" BIGINT,
    "p_CancelledBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_file_size BIGINT;
    v_is_cover BOOLEAN;
    v_count INTEGER;
BEGIN
    SELECT img."FileSize", img."IsCoverImage"
    INTO v_file_size, v_is_cover
    FROM "MemoryImages" img
    INNER JOIN "Memories" m
        ON m."ID_Memories" = img."FK_Memories"
       AND m."FK_Families" = "p_FK_Families"
       AND m."IsCancelled" = FALSE
    WHERE img."ID_MemoryImages" = "p_ID_MemoryImages"
      AND img."FK_Memories" = "p_ID_Memories"
      AND img."FK_Families" = "p_FK_Families"
      AND img."IsCancelled" = FALSE;

    IF NOT FOUND THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Image not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "MemoryImages"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW(),
        "UpdatedBy" = "p_CancelledBy",
        "UpdatedOn" = NOW(),
        "IsCoverImage" = FALSE
    WHERE "ID_MemoryImages" = "p_ID_MemoryImages"
      AND "FK_Memories" = "p_ID_Memories"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    IF COALESCE(v_is_cover, FALSE) THEN
        UPDATE "Memories"
        SET "CoverImageUrl" = NULL,
            "UpdatedBy" = "p_CancelledBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Memories" = "p_ID_Memories"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE;
    END IF;

    SELECT COUNT(*)::INTEGER
    INTO v_count
    FROM "MemoryImages"
    WHERE "FK_Memories" = "p_ID_Memories"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_MemoryImages";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Image deleted successfully.';
    "p_Data" := jsonb_build_object(
        'ImageId', "p_ID_MemoryImages",
        'Url', NULL,
        'FileSize', COALESCE(v_file_size, 0),
        'ImageCount', v_count,
        'MaxImageCount', 10
    );
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
