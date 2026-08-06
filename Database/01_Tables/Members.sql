CREATE TABLE IF NOT EXISTS "Members" (
    "ID_Members"         BIGSERIAL PRIMARY KEY,
    "FK_Families"        BIGINT       NOT NULL,
    "FK_Members_Parent"  BIGINT       NULL,
    "FK_Members_Spouse"  BIGINT       NULL,
    "FirstName"          VARCHAR(100) NOT NULL,
    "LastName"           VARCHAR(100) NOT NULL,
    "Email"              VARCHAR(256) NULL,
    "Phone"              VARCHAR(30)  NULL,
    "Gender"             VARCHAR(20)  NULL,
    "DateOfBirth"        DATE         NULL,
    "DateOfDeath"        DATE         NULL,
    "IsRoot"             BOOLEAN      NOT NULL DEFAULT FALSE,
    "Nickname"           VARCHAR(100) NULL,
    "Biography"          VARCHAR(4000) NULL,
    "Profession"         VARCHAR(200) NULL,
    "CreatedBy"          VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"          TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"          VARCHAR(100) NULL,
    "UpdatedOn"          TIMESTAMPTZ  NULL,
    "IsCancelled"        BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"        VARCHAR(100) NULL,
    "CancelledOn"        TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_Members_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT,
    CONSTRAINT "FK_Members_Parent"
        FOREIGN KEY ("FK_Members_Parent") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT,
    CONSTRAINT "FK_Members_Spouse"
        FOREIGN KEY ("FK_Members_Spouse") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT
);
