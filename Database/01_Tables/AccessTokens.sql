CREATE TABLE IF NOT EXISTS "AccessTokens" (
    "ID_AccessTokens"    BIGSERIAL PRIMARY KEY,
    "FK_Families"        BIGINT       NOT NULL,
    "FK_Members"         BIGINT       NULL,
    "TokenName"          VARCHAR(200) NOT NULL,
    "Permission"         VARCHAR(20)  NOT NULL,
    "Scope"              VARCHAR(40)  NOT NULL,
    "TokenHash"          VARCHAR(500) NOT NULL,
    "TokenPreview"       VARCHAR(80)  NOT NULL,
    "Status"             VARCHAR(20)  NOT NULL DEFAULT 'Active',
    "ExpiresOn"          TIMESTAMPTZ  NOT NULL,
    "LastUsedOn"         TIMESTAMPTZ  NULL,
    "ActiveSessions"     INTEGER      NOT NULL DEFAULT 0,
    "UsageCount"         INTEGER      NOT NULL DEFAULT 0,
    "CreatedBy"          VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"          VARCHAR(100) NULL,
    "UpdatedOn"          TIMESTAMPTZ  NULL,
    "IsCancelled"        BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"        VARCHAR(100) NULL,
    "CancelledOn"        TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_AccessTokens_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT,
    CONSTRAINT "FK_AccessTokens_Members"
        FOREIGN KEY ("FK_Members") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT,
    CONSTRAINT "CK_AccessTokens_Permission"
        CHECK ("Permission" IN ('View', 'Edit')),
    CONSTRAINT "CK_AccessTokens_Scope"
        CHECK ("Scope" IN ('EntireFamily', 'SelectedMember', 'MemberDescendants')),
    CONSTRAINT "CK_AccessTokens_Status"
        CHECK ("Status" IN ('Active', 'Inactive'))
);

CREATE INDEX IF NOT EXISTS "IX_AccessTokens_Family"
    ON "AccessTokens" ("FK_Families")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_AccessTokens_TokenHash"
    ON "AccessTokens" ("TokenHash")
    WHERE "IsCancelled" = FALSE;
