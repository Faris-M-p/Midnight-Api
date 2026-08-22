CREATE OR REPLACE PROCEDURE "ProEventCoverCommit"(
    "p_FK_Families" BIGINT,
    "p_ID_Events" BIGINT,
    "p_CoverImageUrl" VARCHAR,
    "p_CoverStorageKey" VARCHAR,
    "p_CoverFileSize" BIGINT,
    "p_CoverMimeType" VARCHAR,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_old_key VARCHAR;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Events"
        WHERE "ID_Events" = "p_ID_Events"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Event not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_CoverImageUrl" IS NULL OR LENGTH(TRIM("p_CoverImageUrl")) = 0 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Cover image url is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_CoverStorageKey" IS NULL OR LENGTH(TRIM("p_CoverStorageKey")) = 0 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Cover storage key is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF COALESCE("p_CoverFileSize", 0) <= 0 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Cover file size is invalid.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    SELECT "CoverStorageKey"
    INTO v_old_key
    FROM "Events"
    WHERE "ID_Events" = "p_ID_Events"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    UPDATE "Events"
    SET "CoverImageUrl" = TRIM("p_CoverImageUrl"),
        "CoverStorageKey" = TRIM("p_CoverStorageKey"),
        "CoverFileSize" = "p_CoverFileSize",
        "CoverMimeType" = NULLIF(TRIM(COALESCE("p_CoverMimeType", '')), ''),
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Events" = "p_ID_Events"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Events";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Event cover updated successfully.';
    "p_Data" := jsonb_build_object(
        'Id', "p_ID_Events",
        'PreviousStorageKey', v_old_key
    );
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
