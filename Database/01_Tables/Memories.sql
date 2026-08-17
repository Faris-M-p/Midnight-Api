CREATE TABLE IF NOT EXISTS "Memories" (
    "ID_Memories"   BIGSERIAL PRIMARY KEY,
    "FK_Families"   BIGINT        NOT NULL,
    "Title"         VARCHAR(200)  NOT NULL,
    "Description"   TEXT          NULL,
    "MemoryDate"    TIMESTAMPTZ   NOT NULL,
    "Location"      VARCHAR(200)  NULL,
    "CoverImageUrl" VARCHAR(2000) NULL,
    "CreatedBy"     VARCHAR(100)  NOT NULL DEFAULT 'system',
    "CreatedOn"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedBy"     VARCHAR(100)  NULL,
    "UpdatedOn"     TIMESTAMPTZ   NULL,
    "IsCancelled"   BOOLEAN       NOT NULL DEFAULT FALSE,
    "CancelledBy"   VARCHAR(100)  NULL,
    "CancelledOn"   TIMESTAMPTZ   NULL,
    CONSTRAINT "FK_Memories_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT
);

ALTER TABLE "Memories"
    ADD COLUMN IF NOT EXISTS "CoverImageUrl" VARCHAR(2000) NULL;

ALTER TABLE "Memories"
    DROP CONSTRAINT IF EXISTS "FK_Memories_CoverMedia";

ALTER TABLE "Memories"
    DROP COLUMN IF EXISTS "FK_CoverMedia";

CREATE INDEX IF NOT EXISTS "IX_Memories_Family"
    ON "Memories" ("FK_Families")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Memories_MemoryDate"
    ON "Memories" ("MemoryDate")
    WHERE "IsCancelled" = FALSE;
