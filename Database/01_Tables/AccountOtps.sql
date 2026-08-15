CREATE TABLE IF NOT EXISTS "AccountOtps" (
    "ID_AccountOtps"   BIGSERIAL PRIMARY KEY,
    "FK_UserAccounts"  BIGINT       NOT NULL,
    "Email"            VARCHAR(256) NOT NULL,
    "OtpHash"          VARCHAR(500) NOT NULL,
    "Purpose"          VARCHAR(50)  NOT NULL,
    "ExpiresOn"        TIMESTAMPTZ  NOT NULL,
    "AttemptCount"     INTEGER      NOT NULL DEFAULT 0,
    "MaxAttempts"      INTEGER      NOT NULL DEFAULT 5,
    "IsUsed"           BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedOn"        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UsedOn"           TIMESTAMPTZ  NULL,
    "LastSentOn"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ResetTokenHash"   VARCHAR(500) NULL,
    "ResetTokenExpiresOn" TIMESTAMPTZ NULL,
    "IsCancelled"      BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"      VARCHAR(100) NULL,
    "CancelledOn"      TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_AccountOtps_UserAccounts"
        FOREIGN KEY ("FK_UserAccounts") REFERENCES "UserAccounts" ("ID_UserAccounts") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AccountOtps_Account_Purpose"
    ON "AccountOtps" ("FK_UserAccounts", "Purpose")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_AccountOtps_Email_Purpose"
    ON "AccountOtps" ("Email", "Purpose")
    WHERE "IsCancelled" = FALSE;
