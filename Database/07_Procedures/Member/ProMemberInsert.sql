CREATE OR REPLACE FUNCTION "ProMemberInsert"(
    p_family_id   BIGINT,
    p_parent_id   BIGINT,
    p_spouse_id   BIGINT,
    p_first_name  VARCHAR,
    p_last_name   VARCHAR,
    p_email       VARCHAR,
    p_phone       VARCHAR,
    p_gender      VARCHAR,
    p_dob         DATE,
    p_dod         DATE,
    p_is_root     BOOLEAN,
    p_nickname    VARCHAR,
    p_biography   VARCHAR,
    p_profession  VARCHAR,
    p_addresses   JSONB,
    p_images      JSONB,
    p_events      JSONB,
    p_notes       JSONB,
    p_social_links JSONB,
    p_created_by  VARCHAR
)
RETURNS TABLE (
    "ResponseCode"    INTEGER,
    "StatusCode"      INTEGER,
    "ResponseMessage" VARCHAR
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_id BIGINT;
    v_item JSONB;
    v_code INTEGER;
    v_status INTEGER;
    v_message VARCHAR;
BEGIN
    SELECT v."ResponseCode", v."StatusCode", v."ResponseMessage"
    INTO v_code, v_status, v_message
    FROM "FnMemberValidateSave"(p_family_id, NULL, p_parent_id, p_spouse_id, p_is_root) v;

    IF v_code <> 0 THEN
        RETURN QUERY SELECT v_code, v_status, v_message;
        RETURN;
    END IF;

    INSERT INTO "Members" (
        "FK_Families", "FK_Members_Parent", "FirstName", "LastName", "Email", "Phone",
        "Gender", "DateOfBirth", "DateOfDeath", "IsRoot", "Nickname", "Biography",
        "Profession", "CreatedBy", "CreatedOn"
    )
    VALUES (
        p_family_id, p_parent_id, TRIM(p_first_name), TRIM(p_last_name), p_email, p_phone,
        p_gender, p_dob, p_dod, COALESCE(p_is_root, FALSE),
        NULLIF(TRIM(COALESCE(p_nickname, '')), ''), p_biography, p_profession,
        p_created_by, NOW()
    )
    RETURNING "Members"."ID_Members" INTO v_id;

    IF p_addresses IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_addresses)
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
                p_created_by,
                NOW()
            );
        END LOOP;
    END IF;

    IF p_images IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_images)
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
                p_created_by,
                NOW()
            );
        END LOOP;
    END IF;

    IF p_events IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_events)
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
                p_created_by,
                NOW()
            );
        END LOOP;
    END IF;

    IF p_notes IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_notes)
        LOOP
            INSERT INTO "MemberNotes" (
                "FK_Members", "Title", "Content", "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'Title',
                v_item->>'Content',
                p_created_by,
                NOW()
            );
        END LOOP;
    END IF;

    IF p_social_links IS NOT NULL THEN
        FOR v_item IN SELECT * FROM jsonb_array_elements(p_social_links)
        LOOP
            INSERT INTO "MemberSocialLinks" (
                "FK_Members", "Platform", "Url", "Username", "CreatedBy", "CreatedOn"
            )
            VALUES (
                v_id,
                v_item->>'Platform',
                v_item->>'Url',
                v_item->>'Username',
                p_created_by,
                NOW()
            );
        END LOOP;
    END IF;

    IF p_spouse_id IS NOT NULL THEN
        UPDATE "Members"
        SET "FK_Members_Spouse" = p_spouse_id,
            "UpdatedBy" = p_created_by,
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = v_id;

        UPDATE "Members"
        SET "FK_Members_Spouse" = v_id,
            "UpdatedBy" = p_created_by,
            "UpdatedOn" = NOW()
        WHERE "ID_Members" = p_spouse_id
          AND "FK_Families" = p_family_id
          AND "IsCancelled" = FALSE;
    END IF;

    RETURN QUERY SELECT 0, 201, 'Member created successfully.'::VARCHAR;
END;
$$;
