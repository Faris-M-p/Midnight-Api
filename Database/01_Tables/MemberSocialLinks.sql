CREATE TABLE IF NOT EXISTS "MemberSocialLinks" (
    "ID_MemberSocialLinks" BIGSERIAL PRIMARY KEY,
    "FK_Members"           BIGINT        NOT NULL,
    "Platform"             VARCHAR(50)   NOT NULL,
    "Url"                  VARCHAR(2000) NOT NULL,
    "Username"             VARCHAR(100)  NULL,
    "CreatedBy"            VARCHAR(100)  NOT NULL DEFAULT 'system',
    "CreatedOn"            TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedBy"            VARCHAR(100)  NULL,
    "UpdatedOn"            TIMESTAMPTZ   NULL,
    "IsCancelled"          BOOLEAN       NOT NULL DEFAULT FALSE,
    "CancelledBy"          VARCHAR(100)  NULL,
    "CancelledOn"          TIMESTAMPTZ   NULL,
    CONSTRAINT "FK_MemberSocialLinks_Members"
        FOREIGN KEY ("FK_Members") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT
);
