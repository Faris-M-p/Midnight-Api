CREATE OR REPLACE PROCEDURE "ProMemorySelect"(
    "p_FK_Families" BIGINT,
    "p_ID_Memories" BIGINT,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT jsonb_build_object(
        'Id', m."ID_Memories",
        'Title', m."Title",
        'Description', m."Description",
        'MemoryDate', m."MemoryDate",
        'Location', m."Location",
        'CoverImageUrl', m."CoverImageUrl",
        'CoverUrl', m."CoverImageUrl",
        'CoverImageId', c."ID_MemoryImages",
        'Cover', CASE
            WHEN c."ID_MemoryImages" IS NULL THEN NULL
            ELSE jsonb_build_object(
                'Id', c."ID_MemoryImages",
                'FileName', c."FileName",
                'StorageKey', c."StorageKey",
                'ImageUrl', c."ImageUrl",
                'FileUrl', c."ImageUrl",
                'MimeType', c."MimeType",
                'FileSize', c."FileSize"
            )
        END,
        'Images', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', img."ID_MemoryImages",
                    'FileName', img."FileName",
                    'StorageKey', img."StorageKey",
                    'ImageUrl', img."ImageUrl",
                    'FileUrl', img."ImageUrl",
                    'MimeType', img."MimeType",
                    'FileSize', img."FileSize",
                    'SortOrder', img."SortOrder",
                    'IsCoverImage', img."IsCoverImage",
                    'IsCover', img."IsCoverImage"
                )
                ORDER BY img."SortOrder" ASC, img."ID_MemoryImages" ASC
            )
            FROM "MemoryImages" img
            WHERE img."FK_Memories" = m."ID_Memories"
              AND img."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'CreatedOn', m."CreatedOn",
        'UpdatedOn', m."UpdatedOn"
    )
    INTO "p_Data"
    FROM "Memories" m
    LEFT JOIN "MemoryImages" c
        ON c."FK_Memories" = m."ID_Memories"
       AND c."IsCancelled" = FALSE
       AND c."IsCoverImage" = TRUE
    WHERE m."ID_Memories" = "p_ID_Memories"
      AND m."FK_Families" = "p_FK_Families"
      AND m."IsCancelled" = FALSE;

    IF "p_Data" IS NULL THEN
        "p_Data" := NULL;
    END IF;
END;
$$;
