CREATE OR REPLACE PROCEDURE "ProEventDelete"(
    "p_FK_Families" BIGINT,
    "p_ID_Events" BIGINT,
    "p_CancelledBy" VARCHAR,
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

    UPDATE "EventMembers"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW(),
        "UpdatedBy" = "p_CancelledBy",
        "UpdatedOn" = NOW()
    WHERE "FK_Events" = "p_ID_Events"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    UPDATE "Events"
    SET "IsCancelled" = TRUE,
        "CancelledBy" = "p_CancelledBy",
        "CancelledOn" = NOW(),
        "UpdatedBy" = "p_CancelledBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Events" = "p_ID_Events"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Events";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Event deleted successfully.';
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
