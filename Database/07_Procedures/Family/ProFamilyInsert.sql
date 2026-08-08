CREATE OR REPLACE PROCEDURE "ProFamilyInsert"(
    "p_FamilyCode" VARCHAR,
    "p_FamilyName" VARCHAR,
    "p_Description" VARCHAR,
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
    IF EXISTS (
        SELECT 1
        FROM "Families" f
        WHERE f."FamilyCode" = "p_FamilyCode"
          AND f."IsCancelled" = FALSE
    ) THEN
        "p_ResponseCode" := 20;
        "p_Status" := FALSE;
        "p_ResponseMessage" := 'Family code already exists.';
        "p_Data" := NULL;
        RETURN;
    END IF;

    INSERT INTO "Families" ("FamilyCode", "FamilyName", "Description", "CreatedBy", "CreatedOn")
    VALUES ("p_FamilyCode", "p_FamilyName", "p_Description", "p_CreatedBy", NOW())
    RETURNING "ID_Families" INTO v_id;

    "p_ResponseCode" := v_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Family created successfully.';
    "p_Data" := jsonb_build_object('Id', v_id);
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
