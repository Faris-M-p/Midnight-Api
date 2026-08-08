CREATE OR REPLACE PROCEDURE "ProMemberSelect"(
    p_family_id BIGINT,
    p_member_id BIGINT,
    INOUT p_payload JSONB DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT jsonb_build_object(
        'Id', m."ID_Members",
        'FirstName', m."FirstName",
        'LastName', m."LastName",
        'FullName', m."FirstName" || ' ' || m."LastName",
        'Email', m."Email",
        'Phone', m."Phone",
        'Gender', m."Gender",
        'DateOfBirth', m."DateOfBirth",
        'DateOfDeath', m."DateOfDeath",
        'IsRoot', m."IsRoot",
        'Nickname', m."Nickname",
        'Biography', m."Biography",
        'Profession', m."Profession",
        'Parent', (
            SELECT jsonb_build_object(
                'Id', p."ID_Members",
                'FirstName', p."FirstName",
                'LastName', p."LastName",
                'FullName', p."FirstName" || ' ' || p."LastName",
                'Gender', p."Gender",
                'DateOfBirth', p."DateOfBirth",
                'PhotoUrl', (
                    SELECT i."ImageUrl"
                    FROM "MemberImages" i
                    WHERE i."FK_Members" = p."ID_Members" AND i."IsCancelled" = FALSE
                    ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
                    LIMIT 1
                )
            )
            FROM "Members" p
            WHERE p."ID_Members" = m."FK_Members_Parent"
              AND p."IsCancelled" = FALSE
        ),
        'Spouse', (
            SELECT jsonb_build_object(
                'Id', s."ID_Members",
                'FirstName', s."FirstName",
                'LastName', s."LastName",
                'FullName', s."FirstName" || ' ' || s."LastName",
                'Gender', s."Gender",
                'DateOfBirth', s."DateOfBirth",
                'PhotoUrl', (
                    SELECT i."ImageUrl"
                    FROM "MemberImages" i
                    WHERE i."FK_Members" = s."ID_Members" AND i."IsCancelled" = FALSE
                    ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
                    LIMIT 1
                )
            )
            FROM "Members" s
            WHERE s."ID_Members" = m."FK_Members_Spouse"
              AND s."IsCancelled" = FALSE
        ),
        'Children', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', c."ID_Members",
                    'FirstName', c."FirstName",
                    'LastName', c."LastName",
                    'FullName', c."FirstName" || ' ' || c."LastName",
                    'Gender', c."Gender",
                    'DateOfBirth', c."DateOfBirth",
                    'PhotoUrl', (
                        SELECT i."ImageUrl"
                        FROM "MemberImages" i
                        WHERE i."FK_Members" = c."ID_Members" AND i."IsCancelled" = FALSE
                        ORDER BY i."IsPrimary" DESC, i."SortOrder" ASC
                        LIMIT 1
                    )
                )
                ORDER BY c."FirstName", c."LastName"
            )
            FROM "Members" c
            WHERE c."FK_Members_Parent" = m."ID_Members"
              AND c."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'Addresses', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', a."ID_MemberAddresses",
                    'AddressLine1', a."AddressLine1",
                    'AddressLine2', a."AddressLine2",
                    'City', a."City",
                    'State', a."State",
                    'Country', a."Country",
                    'PostalCode', a."PostalCode",
                    'IsPrimary', a."IsPrimary"
                )
            )
            FROM "MemberAddresses" a
            WHERE a."FK_Members" = m."ID_Members"
              AND a."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'Images', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', i."ID_MemberImages",
                    'ImageUrl', i."ImageUrl",
                    'Caption', i."Caption",
                    'IsPrimary', i."IsPrimary",
                    'SortOrder', i."SortOrder"
                )
                ORDER BY i."SortOrder"
            )
            FROM "MemberImages" i
            WHERE i."FK_Members" = m."ID_Members"
              AND i."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'Events', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', e."ID_MemberEvents",
                    'EventType', e."EventType",
                    'Title', e."Title",
                    'Description', e."Description",
                    'EventDate', e."EventDate"
                )
                ORDER BY e."EventDate" DESC
            )
            FROM "MemberEvents" e
            WHERE e."FK_Members" = m."ID_Members"
              AND e."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'Notes', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', n."ID_MemberNotes",
                    'Title', n."Title",
                    'Content', n."Content"
                )
            )
            FROM "MemberNotes" n
            WHERE n."FK_Members" = m."ID_Members"
              AND n."IsCancelled" = FALSE
        ), '[]'::jsonb),
        'SocialLinks', COALESCE((
            SELECT jsonb_agg(
                jsonb_build_object(
                    'Id', s."ID_MemberSocialLinks",
                    'Platform', s."Platform",
                    'Url', s."Url",
                    'Username', s."Username"
                )
            )
            FROM "MemberSocialLinks" s
            WHERE s."FK_Members" = m."ID_Members"
              AND s."IsCancelled" = FALSE
        ), '[]'::jsonb)
    )
    INTO p_payload
    FROM "Members" m
    WHERE m."ID_Members" = p_member_id
      AND m."FK_Families" = p_family_id
      AND m."IsCancelled" = FALSE;
END;
$$;
