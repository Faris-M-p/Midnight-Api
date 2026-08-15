CREATE TABLE IF NOT EXISTS "UserAccounts" (
    "ID_UserAccounts" BIGSERIAL PRIMARY KEY,
    "FK_Families"     BIGINT       NOT NULL,
    "Username"        VARCHAR(100) NOT NULL,
    "Email"           VARCHAR(256) NOT NULL,
    "PasswordHash"    VARCHAR(500) NOT NULL,
    "IsActive"        BOOLEAN      NOT NULL DEFAULT TRUE,
    "CreatedBy"       VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"       VARCHAR(100) NULL,
    "UpdatedOn"       TIMESTAMPTZ  NULL,
    "IsCancelled"     BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"     VARCHAR(100) NULL,
    "CancelledOn"     TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_UserAccounts_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT
);

-- Existing rows receive TRUE; new registrations insert FALSE explicitly.
ALTER TABLE "UserAccounts"
    ADD COLUMN IF NOT EXISTS "EmailVerified" BOOLEAN NOT NULL DEFAULT TRUE;
