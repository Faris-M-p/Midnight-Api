CREATE TABLE IF NOT EXISTS "Events" (
    "ID_Events"   BIGSERIAL PRIMARY KEY,
    "FK_Families" BIGINT       NOT NULL,
    "Title"       VARCHAR(200) NOT NULL,
    "EventType"   VARCHAR(50)  NOT NULL,
    "EventDate"   DATE         NOT NULL,
    "EventTime"   TIME         NULL,
    "Location"    VARCHAR(200) NULL,
    "Description" TEXT         NULL,
    "CreatedBy"   VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"   VARCHAR(100) NULL,
    "UpdatedOn"   TIMESTAMPTZ  NULL,
    "IsCancelled" BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy" VARCHAR(100) NULL,
    "CancelledOn" TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_Events_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_Events_Family"
    ON "Events" ("FK_Families")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Events_EventDate"
    ON "Events" ("EventDate")
    WHERE "IsCancelled" = FALSE;
