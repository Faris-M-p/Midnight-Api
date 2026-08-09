DROP PROCEDURE IF EXISTS "ProMemberUpdate";

CREATE OR REPLACE PROCEDURE "ProMemberUpdate"(
    "p_FK_Families" BIGINT,
    "p_ID_Members" BIGINT,
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
    "p_LocationName" VARCHAR,
    "p_Latitude" DOUBLE PRECISION,
    "p_Longitude" DOUBLE PRECISION,
    "p_Addresses" JSONB,
    "p_Images" JSONB,
    "p_Events" JSONB,
    "p_Notes" JSONB,
    "p_SocialLinks" JSONB,
    "p_UpdatedBy" VARCHAR,
    INOUT "p_ResponseCode" BIGINT DEFAULT 0,
    INOUT "p_Status" BOOLEAN DEFAULT FALSE,
    INOUT "p_ResponseMessage" VARCHAR DEFAULT NULL,
    INOUT "p_Data" JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_item JSONB;
    v_keep BIGINT[];
    v_id BIGINT;
    v_code INTEGER;
    v_ok BOOLEAN;
    v_message VARCHAR;
BEGIN
    SELECT v."ResponseCode", v."Status", v."ResponseMessage"
    INTO v_code, v_ok, v_message
    FROM "FnMemberValidateSave"("p_FK_Families", "p_ID_Members", "p_FK_Members_Parent", "p_FK_Members_Spouse", "p_IsRoot") v;

    IF v_code <> 10 THEN
        "p_ResponseCode" := v_code;
        "p_Status" := v_ok;
        "p_ResponseMessage" := v_message;
        "p_Data" := NULL;
        RETURN;
    END IF;

    UPDATE "Members"
    SET "FK_Members_Parent" = "p_FK_Members_Parent",
        "FirstName" = TRIM("p_FirstName"),
        "LastName" = TRIM("p_LastName"),
        "Email" = "p_Email",
        "Phone" = "p_Phone",
        "Gender" = "p_Gender",
        "DateOfBirth" = "p_DateOfBirth",
        "DateOfDeath" = "p_DateOfDeath",
        "IsRoot" = COALESCE("p_IsRoot", FALSE),
        "Nickname" = NULLIF(TRIM(COALESCE("p_Nickname", '')), ''),
        "Biography" = "p_Biography",
        "Profession" = "p_Profession",
        "LocationName" = NULLIF(TRIM(COALESCE("p_LocationName", '')), ''),
        "Latitude" = "p_Latitude",
        "Longitude" = "p_Longitude",
        "UpdatedBy" = "p_UpdatedBy",
        "UpdatedOn" = NOW()
    WHERE "ID_Members" = "p_ID_Members";

    IF "p_Addresses" IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Addresses")
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberAddresses"
                SET "AddressLine1" = v_item->>'AddressLine1',
                    "AddressLine2" = v_item->>'AddressLine2',
                    "City" = v_item->>'City',
                    "State" = v_item->>'State',
                    "Country" = v_item->>'Country',
                    "PostalCode" = v_item->>'PostalCode',
                    "IsPrimary" = COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    "UpdatedBy" = "p_UpdatedBy",
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberAddresses" = v_id
                  AND "FK_Members" = "p_ID_Members"
                  AND "IsCancelled" = FALSE;

                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Address not found.';
                END IF;
                v_keep := array_append(v_keep, v_id);
            ELSE
                INSERT INTO "MemberAddresses" (
                    "FK_Members", "AddressLine1", "AddressLine2", "City", "State",
                    "Country", "PostalCode", "IsPrimary", "CreatedBy", "CreatedOn"
                )
                VALUES (
                    "p_ID_Members",
                    v_item->>'AddressLine1',
                    v_item->>'AddressLine2',
                    v_item->>'City',
                    v_item->>'State',
                    v_item->>'Country',
                    v_item->>'PostalCode',
                    COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    "p_UpdatedBy",
                    NOW()
                )
                RETURNING "ID_MemberAddresses" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberAddresses"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = "p_UpdatedBy",
            "CancelledOn" = NOW()
        WHERE "FK_Members" = "p_ID_Members"
          AND "IsCancelled" = FALSE
          AND ("ID_MemberAddresses" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF "p_Images" IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Images")
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberImages"
                SET "ImageUrl" = v_item->>'ImageUrl',
                    "Caption" = v_item->>'Caption',
                    "IsPrimary" = COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    "SortOrder" = COALESCE((v_item->>'SortOrder')::INTEGER, 0),
                    "UpdatedBy" = "p_UpdatedBy",
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberImages" = v_id
                  AND "FK_Members" = "p_ID_Members"
                  AND "IsCancelled" = FALSE;

                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Image not found.';
                END IF;
                v_keep := array_append(v_keep, v_id);
            ELSE
                INSERT INTO "MemberImages" (
                    "FK_Members", "ImageUrl", "Caption", "IsPrimary", "SortOrder",
                    "CreatedBy", "CreatedOn"
                )
                VALUES (
                    "p_ID_Members",
                    v_item->>'ImageUrl',
                    v_item->>'Caption',
                    COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    COALESCE((v_item->>'SortOrder')::INTEGER, 0),
                    "p_UpdatedBy",
                    NOW()
                )
                RETURNING "ID_MemberImages" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberImages"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = "p_UpdatedBy",
            "CancelledOn" = NOW()
        WHERE "FK_Members" = "p_ID_Members"
          AND "IsCancelled" = FALSE
          AND ("ID_MemberImages" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF "p_Events" IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Events")
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberEvents"
                SET "EventType" = v_item->>'EventType',
                    "Title" = v_item->>'Title',
                    "Description" = v_item->>'Description',
                    "EventDate" = (v_item->>'EventDate')::DATE,
                    "UpdatedBy" = "p_UpdatedBy",
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberEvents" = v_id
                  AND "FK_Members" = "p_ID_Members"
                  AND "IsCancelled" = FALSE;

                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Event not found.';
                END IF;
                v_keep := array_append(v_keep, v_id);
            ELSE
                INSERT INTO "MemberEvents" (
                    "FK_Members", "EventType", "Title", "Description", "EventDate",
                    "CreatedBy", "CreatedOn"
                )
                VALUES (
                    "p_ID_Members",
                    v_item->>'EventType',
                    v_item->>'Title',
                    v_item->>'Description',
                    (v_item->>'EventDate')::DATE,
                    "p_UpdatedBy",
                    NOW()
                )
                RETURNING "ID_MemberEvents" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberEvents"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = "p_UpdatedBy",
            "CancelledOn" = NOW()
        WHERE "FK_Members" = "p_ID_Members"
          AND "IsCancelled" = FALSE
          AND ("ID_MemberEvents" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF "p_Notes" IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_Notes")
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberNotes"
                SET "Title" = v_item->>'Title',
                    "Content" = v_item->>'Content',
                    "UpdatedBy" = "p_UpdatedBy",
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberNotes" = v_id
                  AND "FK_Members" = "p_ID_Members"
                  AND "IsCancelled" = FALSE;

                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Note not found.';
                END IF;
                v_keep := array_append(v_keep, v_id);
            ELSE
                INSERT INTO "MemberNotes" (
                    "FK_Members", "Title", "Content", "CreatedBy", "CreatedOn"
                )
                VALUES (
                    "p_ID_Members",
                    v_item->>'Title',
                    v_item->>'Content',
                    "p_UpdatedBy",
                    NOW()
                )
                RETURNING "ID_MemberNotes" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberNotes"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = "p_UpdatedBy",
            "CancelledOn" = NOW()
        WHERE "FK_Members" = "p_ID_Members"
          AND "IsCancelled" = FALSE
          AND ("ID_MemberNotes" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF "p_SocialLinks" IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements("p_SocialLinks")
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberSocialLinks"
                SET "Platform" = v_item->>'Platform',
                    "Url" = v_item->>'Url',
                    "Username" = v_item->>'Username',
                    "UpdatedBy" = "p_UpdatedBy",
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberSocialLinks" = v_id
                  AND "FK_Members" = "p_ID_Members"
                  AND "IsCancelled" = FALSE;

                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Social link not found.';
                END IF;
                v_keep := array_append(v_keep, v_id);
            ELSE
                INSERT INTO "MemberSocialLinks" (
                    "FK_Members", "Platform", "Url", "Username", "CreatedBy", "CreatedOn"
                )
                VALUES (
                    "p_ID_Members",
                    v_item->>'Platform',
                    v_item->>'Url',
                    v_item->>'Username',
                    "p_UpdatedBy",
                    NOW()
                )
                RETURNING "ID_MemberSocialLinks" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberSocialLinks"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = "p_UpdatedBy",
            "CancelledOn" = NOW()
        WHERE "FK_Members" = "p_ID_Members"
          AND "IsCancelled" = FALSE
          AND ("ID_MemberSocialLinks" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF "p_FK_Members_Spouse" IS NOT NULL THEN
        UPDATE "Members"
        SET "FK_Members_Spouse" = "p_FK_Members_Spouse",
            "UpdatedBy" = "p_UpdatedBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = "p_ID_Members";

        UPDATE "Members"
        SET "FK_Members_Spouse" = "p_ID_Members",
            "UpdatedBy" = "p_UpdatedBy",
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = "p_FK_Members_Spouse"
          AND "FK_Families" = "p_FK_Families"
          AND "IsCancelled" = FALSE;
    END IF;

    "p_ResponseCode" := "p_ID_Members";
    "p_Status" := TRUE;
    "p_ResponseMessage" := 'Member updated successfully.';
    "p_Data" := jsonb_build_object('Id', "p_ID_Members");
EXCEPTION WHEN OTHERS THEN
    "p_ResponseCode" := -1;
    "p_Status" := FALSE;
    "p_ResponseMessage" := SQLERRM;
    "p_Data" := NULL;
END;
$$;
