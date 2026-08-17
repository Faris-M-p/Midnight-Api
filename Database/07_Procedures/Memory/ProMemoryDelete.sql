CREATE OR REPLACE PROCEDURE "ProMemoryDelete"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    "p_CancelledBy" VARCHAR,
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

    UPDATE "MemoryImages"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW(),
        "UpdatedBy" = "p_CancelledBy",
        "UpdatedOn" = NOW(),
        "IsCoverImage" = FALSE
    WHERE "FK_Memories" = "p_ID_Memories"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    UPDATE "Memories"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW(),
        "UpdatedBy" = "p_CancelledBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Memories" = "p_ID_Memories"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Memories";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Memory deleted successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Memories");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
