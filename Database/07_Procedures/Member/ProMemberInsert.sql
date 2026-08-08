CREATE OR REPLACE PROCEDURE "ProMemberInsert"(
    "p_FK_Families" BIGINT,
    "p_FK_Members_Parent" BIGINT,
    "p_FK_Members_Spouse" BIGINT,
    "p_FirstName" VARCHAR,
    "p_LastName" VARCHAR,
    "p_Email" VARCHAR,
    "p_Phone" VARCHAR,
    "p_Gender" VARCHAR,
    "p_DateOfBirth" DATE,
    "p_DateOfDeath" DATE,
    "p_IsRoot" BOOLEAN,
    "p_Nickname" VARCHAR,
    "p_Biography" VARCHAR,
    "p_Profession" VARCHAR,
    "p_Addresses" JSONB,
    "p_Images" JSONB,
    "p_Events" JSONB,
    "p_Notes" JSONB,
    "p_SocialLinks" JSONB,
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
    v_item JSONB;
    v_code INTEGER;
    v_ok BOOLEAN;
    v_message VARCHAR;
BEGIN
    SELECT v."ResponseCode", v."Status", v."ResponseMessage"
    INTO v_code, v_ok, v_message
    FROM "FnMemberValidateSave"("p_FK_Families", NULL, "p_FK_Members_Parent", "p_FK_Members_Spouse", "p_IsRoot") v;

    IF v_code <> 10 THEN
        "p_ResponseCode" := v_code;
        "p_Status" := v_ok;
        "p_ResponseMessage" := v_message;
        "p_Data" := NULL;
        RETURN;
    END IF;

    INSERT INTO "Members" (
        "FK_Families", "FK_Members_Parent", "FirstName", "LastName", "Email", "Phone",
        "Gender", "DateOfBirth", "DateOfDeath", "IsRoot", "Nickname", "Biography",
        "Profession", "CreatedBy", "CreatedOn"
    )
    VALUES (
        "p_FK_Families", "p_FK_Members_Parent", TRIM("p_FirstName"), TRIM("p_LastName"), "p_Email", "p_Phone",
        "p_Gender", "p_DateOfBirth", "p_DateOfDeath", COALESCE("p_IsRoot", FALSE),
        NULLIF(TRIM(COALESCE("p_Nickname", '')), ''), "p_Biography", "p_Profession",
        "p_CreatedBy", NOW()
    )
    RETURNING "Members"."ID_Members" INTO v_id;

    IF "p_Addresses" IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Addresses")
        LOOP
            INSERT INTO "MemberAddresses" (
                "FK_Members", "AddressLine1", "AddressLine2", "City", "State",
                "Country", "PostalCode", "IsPrimary", "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'AddressLine1',
                v_item->>'AddressLine2',
                v_item->>'City',
                v_item->>'State',
                v_item->>'Country',
                v_item->>'PostalCode',
                COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                "p_CreatedBy",
                NOW()
            );
        END LOOP;
    END IF;

    IF "p_Images" IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Images")
        LOOP
            INSERT INTO "MemberImages" (
                "FK_Members", "ImageUrl", "Caption", "IsPrimary", "SortOrder",
                "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'ImageUrl',
                v_item->>'Caption',
                COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                COALESCE((v_item->>'SortOrder')::INTEGER, 0),
                "p_CreatedBy",
                NOW()
            );
        END LOOP;
    END IF;

    IF "p_Events" IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Events")
        LOOP
            INSERT INTO "MemberEvents" (
                "FK_Members", "EventType", "Title", "Description", "EventDate",
                "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'EventType',
                v_item->>'Title',
                v_item->>'Description',
                (v_item->>'EventDate')::DATE,
                "p_CreatedBy",
                NOW()
            );
        END LOOP;
    END IF;

    IF "p_Notes" IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Notes")
        LOOP
            INSERT INTO "MemberNotes" (
                "FK_Members", "Title", "Content", "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'Title',
                v_item->>'Content',
                "p_CreatedBy",
                NOW()
            );
        END LOOP;
    END IF;

    IF "p_SocialLinks" IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_SocialLinks")
        LOOP
            INSERT INTO "MemberSocialLinks" (
                "FK_Members", "Platform", "Url", "Username", "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'Platform',
                v_item->>'Url',
                v_item->>'Username',
                "p_CreatedBy",
                NOW()
            );
        END LOOP;
    END IF;

    IF "p_FK_Members_Spouse" IS NOT NULL THEN
        UPDATE "Members"
        SET "FK_Members_Spouse" = "p_FK_Members_Spouse",
            "UpdatedBy" = "p_CreatedBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = v_id;

        UPDATE "Members"
        SET "FK_Members_Spouse" = v_id,
            "UpdatedBy" = "p_CreatedBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = "p_FK_Members_Spouse"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE;
    END IF;

    "p_ResponseCode" := v_id;
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Member created successfully.';
    "p_Data" := jsonb_build_object('Id', v_id);
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
