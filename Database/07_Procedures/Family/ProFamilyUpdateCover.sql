CREATE OR REPLACE PROCEDURE "ProFamilyUpdateCover"(
    "p_ID_Families" BIGINT,
    "p_CoverUrl" VARCHAR,
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
        SELECT 1
        FROM "Families" f
        WHERE f."ID_Families" = "p_ID_Families"
          AND f."IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Family not found.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "Families" f
    SET "CoverUrl" = "p_CoverUrl",
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE f."ID_Families" = "p_ID_Families"
      AND f."IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Families";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Family cover updated successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Families");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
