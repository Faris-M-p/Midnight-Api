CREATE TABLE IF NOT EXISTS "MemberNotes" (
    "ID_MemberNotes" BIGSERIAL PRIMARY KEY,
    "FK_Members"     BIGINT        NOT NULL,
    "Title"          VARCHAR(200)  NOT NULL,
    "Content"        VARCHAR(4000) NOT NULL,
    "CreatedBy"      VARCHAR(100)  NOT NULL DEFAULT 'system',
    "CreatedOn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedBy"      VARCHAR(100)  NULL,
    "UpdatedOn"      TIMESTAMPTZ   NULL,
    "IsCancelled"    BOOLEAN       NOT NULL DEFAULT FALSE,
    "CancelledBy"    VARCHAR(100)  NULL,
    "CancelledOn"    TIMESTAMPTZ   NULL,
    CONSTRAINT "FK_MemberNotes_Members"
        FOREIGN KEY ("FK_Members") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT
);
