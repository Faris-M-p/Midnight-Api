CREATE OR REPLACE PROCEDURE "ProMemoryImageUploadContext"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_count INTEGER;
    v_next_sort INTEGER;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "Memories"
        WHERE "ID_Memories" = "p_ID_Memories"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE
    ) THEN
        "p_Data" := NULL;
        RETURN;
    END IF;

    SELECT COUNT(*)::INTEGER
    INTO v_count
    FROM "MemoryImages"
    WHERE "FK_Memories" = "p_ID_Memories"
      AND "IsCancelled" = FALSE;

    SELECT COALESCE(MAX("SortOrder"), -1) + 1
    INTO v_next_sort
    FROM "MemoryImages"
    WHERE "FK_Memories" = "p_ID_Memories"
      AND "IsCancelled" = FALSE;

    "p_Data" := jsonb_build_object(
        'MemoryId', "p_ID_Memories",
        'ImageCount', v_count,
        'MaxImageCount', 10,
        'NextSortOrder', v_next_sort
    );
END;
$$;
