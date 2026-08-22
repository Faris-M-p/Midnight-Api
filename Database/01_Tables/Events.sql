CREATE TABLE IF NOT EXISTS "Events" (
    "ID_Events"       BIGSERIAL PRIMARY KEY,
    "FK_Families"     BIGINT        NOT NULL,
    "Title"           VARCHAR(200)  NOT NULL,
    "EventType"       VARCHAR(50)   NOT NULL,
    "EventDateTime"   TIMESTAMPTZ   NOT NULL,
    "LocationName"    VARCHAR(500)  NULL,
    "Latitude"        DOUBLE PRECISION NULL,
    "Longitude"       DOUBLE PRECISION NULL,
    "Description"     TEXT          NULL,
    "CoverImageUrl"   VARCHAR(2000) NULL,
    "CoverStorageKey" VARCHAR(500)  NULL,
    "CoverFileSize"   BIGINT        NOT NULL DEFAULT 0,
    "CoverMimeType"   VARCHAR(100)  NULL,
    "CreatedBy"       VARCHAR(100)  NOT NULL DEFAULT 'system',
    "CreatedOn"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedBy"       VARCHAR(100)  NULL,
    "UpdatedOn"       TIMESTAMPTZ   NULL,
    "IsCancelled"     BOOLEAN       NOT NULL DEFAULT FALSE,
    "CancelledBy"     VARCHAR(100)  NULL,
    "CancelledOn"     TIMESTAMPTZ   NULL,
    CONSTRAINT "FK_Events_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT
);

-- Legacy columns from earlier schema (migrate then drop).
ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "EventDate" DATE NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "EventTime" TIME NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "Location" VARCHAR(200) NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "EventDateTime" TIMESTAMPTZ NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "LocationName" VARCHAR(500) NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "Latitude" DOUBLE PRECISION NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "Longitude" DOUBLE PRECISION NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "CoverImageUrl" VARCHAR(2000) NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "CoverStorageKey" VARCHAR(500) NULL;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "CoverFileSize" BIGINT NOT NULL DEFAULT 0;

ALTER TABLE "Events"
    ADD COLUMN IF NOT EXISTS "CoverMimeType" VARCHAR(100) NULL;

UPDATE "Events"
SET "EventDateTime" = (
    ("EventDate"::TEXT || ' ' || COALESCE(to_char("EventTime", 'HH24:MI:SS'), '00:00:00'))::TIMESTAMP AT TIME ZONE 'UTC'
)
WHERE "EventDateTime" IS NULL
  AND "EventDate" IS NOT NULL;

UPDATE "Events"
SET "LocationName" = NULLIF(TRIM("Location"), '')
WHERE ("LocationName" IS NULL OR TRIM("LocationName") = '')
  AND "Location" IS NOT NULL
  AND TRIM("Location") <> '';

ALTER TABLE "Events"
    DROP COLUMN IF EXISTS "EventDate";

ALTER TABLE "Events"
    DROP COLUMN IF EXISTS "EventTime";

ALTER TABLE "Events"
    DROP COLUMN IF EXISTS "Location";

DROP INDEX IF EXISTS "IX_Events_EventDate";

CREATE INDEX IF NOT EXISTS "IX_Events_Family"
    ON "Events" ("FK_Families")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Events_EventDateTime"
    ON "Events" ("EventDateTime")
    WHERE "IsCancelled" = FALSE;
