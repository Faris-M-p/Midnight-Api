DROP PROCEDURE IF EXISTS "ProMemoryImageCommit"(BIGINT, BIGINT, VARCHAR, VARCHAR, TEXT, VARCHAR, VARCHAR, BIGINT, INTEGER, BOOLEAN, VARCHAR, BIGINT, BOOLEAN, VARCHAR, JSONB);
DROP PROCEDURE IF EXISTS "ProMemoryImageCommit";

CREATE OR REPLACE PROCEDURE "ProMemoryImageCommit"(
    "p_FK_Families" BIGINT,
    "p_FK_Memories" BIGINT,
    "p_FileName" VARCHAR,
    "p_StorageKey" VARCHAR,
    "p_ImageUrl" VARCHAR,
    "p_MimeType" VARCHAR,
    "p_FileSize" BIGINT,
    "p_SortOrder" INTEGER,
    "p_SetAsCover" BOOLEAN,
    "p_CreatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_image_id BIGINT;
    v_sort INTEGER := COALESCE("p_SortOrder", 0);
    v_count INTEGER;
    v_set_cover BOOLEAN := COALESCE("p_SetAsCover", FALSE);
BEGIN
    IF "p_FileSize" IS NULL OR "p_FileSize" <= 0 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'File size is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Memories"
        WHERE "ID_Memories" = "p_FK_Memories"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Memory not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Families"
        WHERE "ID_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Family not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    SELECT COUNT(*)::INTEGER
    INTO v_count
    FROM "MemoryImages"
    WHERE "FK_Memories" = "p_FK_Memories"
      AND "IsCancelled" = FALSE;

    IF v_count >= 10 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'A memory can have at most 10 images.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF v_set_cover THEN
        UPDATE "MemoryImages"
        SET "IsCoverImage" = FALSE,
            "UpdatedBy" = "p_CreatedBy",
            "UpdatedOn" = NOW()
        WHERE "FK_Memories" = "p_FK_Memories"
          AND "IsCancelled" = FALSE
          AND "IsCoverImage" = TRUE;
    END IF;

    INSERT INTO "MemoryImages" (
        "FK_Memories", "FK_Families", "ImageUrl", "StorageKey", "FileName",
        "MimeType", "FileSize", "SortOrder", "IsCoverImage",
        "CreatedBy", "CreatedOn"
    )
    VALUES (
        "p_FK_Memories", "p_FK_Families", "p_ImageUrl", "p_StorageKey", "p_FileName",
        "p_MimeType", "p_FileSize", v_sort, v_set_cover,
        "p_CreatedBy", NOW()
    )
    RETURNING "ID_MemoryImages" INTO v_image_id;

    IF v_set_cover THEN
        UPDATE "Memories"
        SET "CoverImageUrl" = "p_ImageUrl",
            "UpdatedBy" = "p_CreatedBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Memories" = "p_FK_Memories"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE;
    END IF;

    "p_ResponseCode" := v_image_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Image uploaded successfully.';
    "p_Data" := jsonb_build_object(
        'ImageId', v_image_id,
        'Url', "p_ImageUrl",
        'FileSize', "p_FileSize",
        'ImageCount', v_count + 1,
        'MaxImageCount', 10
    );
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
