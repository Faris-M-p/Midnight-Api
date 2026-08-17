CREATE OR REPLACE PROCEDURE "ProMemoryUpdate"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    "p_Title" VARCHAR,
    "p_Description" TEXT,
    "p_MemoryDate" TIMESTAMPTZ,
    "p_Location" VARCHAR,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
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

    IF "p_Title" IS NULL OR LENGTH(TRIM("p_Title")) = 0 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Title is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_MemoryDate" IS NULL THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Memory date is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "Memories"
    SET "Title" = TRIM("p_Title"),
        "Description" = NULLIF(TRIM(COALESCE("p_Description", '')), ''),
        "MemoryDate" = "p_MemoryDate",
        "Location" = NULLIF(TRIM(COALESCE("p_Location", '')), ''),
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Memories" = "p_ID_Memories"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Memories";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Memory updated successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Memories");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
