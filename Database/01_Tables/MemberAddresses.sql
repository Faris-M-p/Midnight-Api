CREATE TABLE IF NOT EXISTS "MemberAddresses" (
    "ID_MemberAddresses" BIGSERIAL PRIMARY KEY,
    "FK_Members"         BIGINT       NOT NULL,
    "AddressLine1"       VARCHAR(300) NOT NULL,
    "AddressLine2"       VARCHAR(300) NULL,
    "City"               VARCHAR(100) NOT NULL,
    "State"              VARCHAR(100) NULL,
    "Country"            VARCHAR(100) NOT NULL,
    "PostalCode"         VARCHAR(20)  NULL,
    "IsPrimary"          BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedBy"          VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"          VARCHAR(100) NULL,
    "UpdatedOn"          TIMESTAMPTZ  NULL,
    "IsCancelled"        BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"        VARCHAR(100) NULL,
    "CancelledOn"        TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_MemberAddresses_Members"
        FOREIGN KEY ("FK_Members") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT
);
