CREATE OR REPLACE PROCEDURE "ProMemberMapSpouse"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
    "p_FK_Members_Spouse" BIGINT,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_code INTEGER;
    v_ok BOOLEAN;
    v_message VARCHAR;
BEGIN
    SELECT v."ResponseCode", v."Status", v."ResponseMessage"
    INTO v_code, v_ok, v_message
    FROM "FnMemberValidateMapSpouse"("p_FK_Families", "p_ID_Members", "p_FK_Members_Spouse") v;

    IF v_code <> 10 THEN
        "p_ResponseCode" := v_code;
        "p_Status" := v_ok;
        "p_ResponseMessage" := v_message;
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "Members"
    SET "FK_Members_Spouse" = "p_FK_Members_Spouse",
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Members" = "p_ID_Members"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    UPDATE "Members"
    SET "FK_Members_Spouse" = "p_ID_Members",
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Members" = "p_FK_Members_Spouse"
      AND "FK_Families" = "p_FK_Families"
      AND "IsCancelled" = FALSE;

    "p_ResponseCode" := "p_ID_Members";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Spouse relationship mapped successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Members");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
