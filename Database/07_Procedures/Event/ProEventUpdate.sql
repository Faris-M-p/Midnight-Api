CREATE OR REPLACE PROCEDURE "ProEventUpdate"(
    "p_FK_Families" BIGINT,
    "p_ID_Events" BIGINT,
    "p_Title" VARCHAR,
    "p_EventType" VARCHAR,
    "p_EventDateTime" TIMESTAMPTZ,
    "p_LocationName" VARCHAR,
    "p_Latitude" DOUBLE PRECISION,
    "p_Longitude" DOUBLE PRECISION,
    "p_Description" TEXT,
    "p_RemoveCover" BOOLEAN,
    "p_CoverImageUrl" VARCHAR,
    "p_CoverStorageKey" VARCHAR,
    "p_CoverFileSize" BIGINT,
    "p_CoverMimeType" VARCHAR,
    "p_MemberIds" JSONB,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_member_id BIGINT;
    v_member_ids BIGINT[] := ARRAY[]::BIGINT[];
    v_replace_cover BOOLEAN := FALSE;
    v_old_key VARCHAR;
BEGIN
    SELECT e."CoverStorageKey"
    INTO v_old_key
    FROM "Events" e
    WHERE e."ID_Events" = "p_ID_Events"
      AND e."FK_Families" = "p_FK_Families"
      AND e."IsCancelled" = FALSE;

    IF NOT FOUND THEN
        "p_ResponseCode" := 30;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Event not found.';
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

    IF "p_EventType" IS NULL OR LENGTH(TRIM("p_EventType")) = 0 THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Event type is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    IF "p_EventDateTime" IS NULL THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Event date and time is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    v_replace_cover := COALESCE("p_RemoveCover", FALSE) = FALSE
        AND NULLIF(TRIM(COALESCE("p_CoverStorageKey", '')), '') IS NOT NULL;

    IF v_replace_cover THEN
        IF "p_CoverImageUrl" IS NULL OR LENGTH(TRIM("p_CoverImageUrl")) = 0 THEN
            "p_ResponseCode" := -1;
            "p_Status" := FALSE;
            "p_ResponseMessage" := 'Cover image url is required.';
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
    END IF;

    IF "p_MemberIds" IS NOT NULL AND jsonb_typeof("p_MemberIds") = 'array' THEN
        FOR v_member_id IN
            SELECT DISTINCT (value)::BIGINT
            FROM jsonb_array_elements_text("p_MemberIds") AS t(value)
            WHERE NULLIF(TRIM(value), '') IS NOT NULL
        LOOP
            IF v_member_id IS NULL OR v_member_id <= 0 THEN
                "p_ResponseCode" := -1;
                "p_Status" := FALSE;
                "p_ResponseMessage" := 'Member id is invalid.';
                "p_Data" := NULL;
                RETURN;
            END IF;

            IF NOT EXISTS (
                SELECT 1 FROM "Members"
                WHERE "ID_Members" = v_member_id
                  AND "FK_Families" = "p_FK_Families"
                  AND "IsCancelled" = FALSE
            ) THEN
                "p_ResponseCode" := -1;
                "p_Status" := FALSE;
                "p_ResponseMessage" := 'One or more members were not found in this family.';
                "p_Data" := NULL;
                RETURN;
            END IF;

            v_member_ids := array_append(v_member_ids, v_member_id);
        END LOOP;
    END IF;

    UPDATE "Events"
    SET "Title" = TRIM("p_Title"),
        "EventType" = LOWER(TRIM("p_EventType")),
        "EventDateTime" = "p_EventDateTime",
        "LocationName" = NULLIF(TRIM(COALESCE("p_LocationName", '')), ''),
        "Latitude" = "p_Latitude",
        "Longitude" = "p_Longitude",
        "Description" = NULLIF(TRIM(COALESCE("p_Description", '')), ''),
        "CoverImageUrl" = CASE
            WHEN COALESCE("p_RemoveCover", FALSE) THEN NULL
            WHEN v_replace_cover THEN TRIM("p_CoverImageUrl")
            ELSE "CoverImageUrl"
        END,
        "CoverStorageKey" = CASE
            WHEN COALESCE("p_RemoveCover", FALSE) THEN NULL
            WHEN v_replace_cover THEN TRIM("p_CoverStorageKey")
            ELSE "CoverStorageKey"
        END,
        "CoverFileSize" = CASE
            WHEN COALESCE("p_RemoveCover", FALSE) THEN 0
            WHEN v_replace_cover THEN "p_CoverFileSize"
            ELSE "CoverFileSize"
        END,
        "CoverMimeType" = CASE
            WHEN COALESCE("p_RemoveCover", FALSE) THEN NULL
            WHEN v_replace_cover THEN NULLIF(TRIM(COALESCE("p_CoverMimeType", '')), '')
            ELSE "CoverMimeType"
        END,
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Events" = "p_ID_Events"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    UPDATE "EventMembers"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_UpdatedBy",
        "CancelledOn" = NOW(),
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "FK_Events" = "p_ID_Events"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    IF array_length(v_member_ids, 1) IS NOT NULL THEN
        FOREACH v_member_id IN ARRAY v_member_ids
        LOOP
            INSERT INTO "EventMembers" (
                "FK_Events", "FK_Members", "FK_Families", "CreatedBy", "CreatedOn"
            )
            VALUES (
                "p_ID_Events", v_member_id, "p_FK_Families", "p_UpdatedBy", NOW()
            );
        END LOOP;
    END IF;

    SELECT jsonb_build_object(
        'Id', e."ID_Events",
        'Title', e."Title",
        'EventType', e."EventType",
        'EventDateTime', e."EventDateTime",
        'LocationName', e."LocationName",
        'Latitude', e."Latitude",
        'Longitude', e."Longitude",
        'Description', e."Description",
        'CoverImageUrl', e."CoverImageUrl",
        'CoverStorageKey', e."CoverStorageKey",
        'CoverFileSize', e."CoverFileSize",
        'CoverMimeType', e."CoverMimeType",
        'PreviousStorageKey', v_old_key,
        'Members', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', m."ID_Members",
                    'FirstName', m."FirstName",
                    'LastName', m."LastName",
                    'FullName', TRIM(CONCAT(COALESCE(m."FirstName", ''), ' ', COALESCE(m."LastName", ''))),
                    'PhotoUrl', (
                        SELECT img."ImageUrl"
                        FROM "MemberImages" img
                        WHERE img."FK_Members" = m."ID_Members"
                          AND img."IsCancelled" = FALSE
                        ORDER BY img."IsPrimary" DESC, img."SortOrder" ASC, img."ID_MemberImages" ASC
                        LIMIT 1
                    )
                )
                ORDER BY LOWER(m."FirstName"), LOWER(m."LastName"), m."ID_Members"
            )
            FROM "EventMembers" em
            INNER JOIN "Members" m
                ON m."ID_Members" = em."FK_Members"
               AND m."FK_Families" = e."FK_Families"
               AND m."IsCancelled" = FALSE
            WHERE em."FK_Events" = e."ID_Events"
              AND em."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'CreatedOn', e."CreatedOn",
        'UpdatedOn', e."UpdatedOn"
    )
    INTO "p_Data"
    FROM "Events" e
    WHERE e."ID_Events" = "p_ID_Events"
      AND e."FK_Families" = "p_FK_Families"
      AND e."IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Events";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Event updated successfully.';
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
