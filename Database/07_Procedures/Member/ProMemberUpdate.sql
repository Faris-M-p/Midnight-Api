CREATE OR REPLACE FUNCTION "ProMemberUpdate"(
    p_family_id    BIGINT,
    p_member_id    BIGINT,
    p_parent_id    BIGINT,
    p_spouse_id    BIGINT,
    p_first_name   VARCHAR,
    p_last_name    VARCHAR,
    p_email        VARCHAR,
    p_phone        VARCHAR,
    p_gender       VARCHAR,
    p_dob          DATE,
    p_dod          DATE,
    p_is_root      BOOLEAN,
    p_nickname     VARCHAR,
    p_biography    VARCHAR,
    p_profession   VARCHAR,
    p_addresses    JSONB,
    p_images       JSONB,
    p_events       JSONB,
    p_notes        JSONB,
    p_social_links JSONB,
    p_updated_by   VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_item JSONB;
    v_keep BIGINT[];
    v_id BIGINT;
    v_code INTEGER;
    v_status INTEGER;
    v_message VARCHAR;
BEGIN
    SELECT v."ResponseCode", v."StatusCode", v."ResponseMessage"
    INTO v_code, v_status, v_message
    FROM "FnMemberValidateSave"(p_family_id, p_member_id, p_parent_id, p_spouse_id, p_is_root) v;

    IF v_code <> 0 THEN
        RETURN QUERY SELECT v_code, v_status, v_message;
        RETURN;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM "Members"
        WHERE "ID_Members" = p_member_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE
    ) THEN
        RETURN QUERY SELECT 1, 404, 'Member not found.'::VARCHAR;
        RETURN;
    END IF;

    UPDATE "Members"
    SET "FK_Members_Parent" = p_parent_id,
        "FirstName" = TRIM(p_first_name),
        "LastName" = TRIM(p_last_name),
        "Email" = p_email,
        "Phone" = p_phone,
        "Gender" = p_gender,
        "DateOfBirth" = p_dob,
        "DateOfDeath" = p_dod,
        "IsRoot" = COALESCE(p_is_root, FALSE),
        "Nickname" = NULLIF(TRIM(COALESCE(p_nickname, '')), ''),
        "Biography" = p_biography,
        "Profession" = p_profession,
        "UpdatedBy" = p_updated_by,
        "UpdatedOn" = NOW()
    WHERE "ID_Members" = p_member_id;

    IF p_addresses IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_addresses)
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
                    "UpdatedBy" = p_updated_by,
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberAddresses" = v_id
                  AND "FK_Members" = p_member_id
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
                    p_member_id,
                    v_item->>'AddressLine1',
                    v_item->>'AddressLine2',
                    v_item->>'City',
                    v_item->>'State',
                    v_item->>'Country',
                    v_item->>'PostalCode',
                    COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    p_updated_by,
                    NOW()
                )
                RETURNING "ID_MemberAddresses" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberAddresses"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = p_updated_by,
            "CancelledOn" = NOW()
        WHERE "FK_Members" = p_member_id
          AND "IsCancelled" = FALSE
          AND ("ID_MemberAddresses" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF p_images IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_images)
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberImages"
                SET "ImageUrl" = v_item->>'ImageUrl',
                    "Caption" = v_item->>'Caption',
                    "IsPrimary" = COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    "SortOrder" = COALESCE((v_item->>'SortOrder')::INTEGER, 0),
                    "UpdatedBy" = p_updated_by,
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberImages" = v_id
                  AND "FK_Members" = p_member_id
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
                    p_member_id,
                    v_item->>'ImageUrl',
                    v_item->>'Caption',
                    COALESCE((v_item->>'IsPrimary')::BOOLEAN, FALSE),
                    COALESCE((v_item->>'SortOrder')::INTEGER, 0),
                    p_updated_by,
                    NOW()
                )
                RETURNING "ID_MemberImages" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberImages"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = p_updated_by,
            "CancelledOn" = NOW()
        WHERE "FK_Members" = p_member_id
          AND "IsCancelled" = FALSE
          AND ("ID_MemberImages" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF p_events IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_events)
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberEvents"
                SET "EventType" = v_item->>'EventType',
                    "Title" = v_item->>'Title',
                    "Description" = v_item->>'Description',
                    "EventDate" = (v_item->>'EventDate')::DATE,
                    "UpdatedBy" = p_updated_by,
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberEvents" = v_id
                  AND "FK_Members" = p_member_id
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
                    p_member_id,
                    v_item->>'EventType',
                    v_item->>'Title',
                    v_item->>'Description',
                    (v_item->>'EventDate')::DATE,
                    p_updated_by,
                    NOW()
                )
                RETURNING "ID_MemberEvents" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberEvents"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = p_updated_by,
            "CancelledOn" = NOW()
        WHERE "FK_Members" = p_member_id
          AND "IsCancelled" = FALSE
          AND ("ID_MemberEvents" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF p_notes IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_notes)
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberNotes"
                SET "Title" = v_item->>'Title',
                    "Content" = v_item->>'Content',
                    "UpdatedBy" = p_updated_by,
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberNotes" = v_id
                  AND "FK_Members" = p_member_id
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
                    p_member_id,
                    v_item->>'Title',
                    v_item->>'Content',
                    p_updated_by,
                    NOW()
                )
                RETURNING "ID_MemberNotes" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberNotes"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = p_updated_by,
            "CancelledOn" = NOW()
        WHERE "FK_Members" = p_member_id
          AND "IsCancelled" = FALSE
          AND ("ID_MemberNotes" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF p_social_links IS NOT NULL THEN
        v_keep := ARRAY[]::BIGINT[];
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_social_links)
        LOOP
            v_id := NULLIF(v_item->>'Id', '')::BIGINT;
            IF v_id IS NOT NULL AND v_id > 0 THEN
                UPDATE "MemberSocialLinks"
                SET "Platform" = v_item->>'Platform',
                    "Url" = v_item->>'Url',
                    "Username" = v_item->>'Username',
                    "UpdatedBy" = p_updated_by,
                    "UpdatedOn" = NOW()
                WHERE "ID_MemberSocialLinks" = v_id
                  AND "FK_Members" = p_member_id
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
                    p_member_id,
                    v_item->>'Platform',
                    v_item->>'Url',
                    v_item->>'Username',
                    p_updated_by,
                    NOW()
                )
                RETURNING "ID_MemberSocialLinks" INTO v_id;
                v_keep := array_append(v_keep, v_id);
            END IF;
        END LOOP;

        UPDATE "MemberSocialLinks"
        SET "IsCancelled" = TRUE,
            "CancelledBy" = p_updated_by,
            "CancelledOn" = NOW()
        WHERE "FK_Members" = p_member_id
          AND "IsCancelled" = FALSE
          AND ("ID_MemberSocialLinks" <> ALL (v_keep) OR cardinality(v_keep) = 0);
    END IF;

    IF p_spouse_id IS NOT NULL THEN
        UPDATE "Members"
        SET "FK_Members_Spouse" = p_spouse_id,
            "UpdatedBy" = p_updated_by,
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = p_member_id;

        UPDATE "Members"
        SET "FK_Members_Spouse" = p_member_id,
            "UpdatedBy" = p_updated_by,
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = p_spouse_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE;
    END IF;

    RETURN QUERY SELECT 0, 200, 'Member updated successfully.'::VARCHAR;
END;
$$;
