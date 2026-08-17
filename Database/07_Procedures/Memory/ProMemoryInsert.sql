CREATE OR REPLACE PROCEDURE "ProMemoryInsert"(
    "p_FK_Families" BIGINT,
    "p_Title" VARCHAR,
    "p_Description" TEXT,
    "p_MemoryDate" TIMESTAMPTZ,
    "p_Location" VARCHAR,
    "p_CreatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id BIGINT;
BEGIN
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

    INSERT INTO "Memories" (
        "FK_Families", "Title", "Description", "MemoryDate", "Location",
        "CreatedBy", "CreatedOn"
    )
    VALUES (
        "p_FK_Families",
        TRIM("p_Title"),
        NULLIF(TRIM(COALESCE("p_Description", '')), ''),
        "p_MemoryDate",
        NULLIF(TRIM(COALESCE("p_Location", '')), ''),
        "p_CreatedBy",
        NOW()
    )
    RETURNING "ID_Memories" INTO v_id;

    "p_ResponseCode" := v_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Memory created successfully.';
    "p_Data" := jsonb_build_object('Id', v_id);
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
