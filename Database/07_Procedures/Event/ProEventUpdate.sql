CREATE OR REPLACE PROCEDURE "ProEventUpdate"(
    "p_FK_Families" BIGINT,
    "p_ID_Events" BIGINT,
    "p_Title" VARCHAR,
    "p_EventType" VARCHAR,
    "p_EventDate" DATE,
    "p_EventTime" VARCHAR,
    "p_Location" VARCHAR,
    "p_Description" TEXT,
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
    v_time TIME;
    v_member_ids BIGINT[] := ARRAY[]::BIGINT[];
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

    IF "p_EventDate" IS NULL THEN
        "p_ResponseCode" := -1;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Event date is required.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    v_time := NULL;
    IF "p_EventTime" IS NOT NULL AND LENGTH(TRIM("p_EventTime")) > 0 THEN
        BEGIN
            v_time := TRIM("p_EventTime")::TIME;
        EXCEPTION WHEN OTHERS THEN
            "p_ResponseCode" := -1;
            "p_Status" := FALSE;
            "p_ResponseMessage" := 'Event time is invalid.';
            "p_Data" := NULL;
            RETURN;
        END;
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
        "EventDate" = "p_EventDate",
        "EventTime" = v_time,
        "Location" = NULLIF(TRIM(COALESCE("p_Location", '')), ''),
        "Description" = NULLIF(TRIM(COALESCE("p_Description", '')), ''),
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

    "p_ResponseCode" := "p_ID_Events";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Event updated successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Events");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
