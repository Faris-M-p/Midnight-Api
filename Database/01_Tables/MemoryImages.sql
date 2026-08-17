CREATE TABLE IF NOT EXISTS "MemoryImages" (
    "ID_MemoryImages" BIGSERIAL PRIMARY KEY,
    "FK_Memories"     BIGINT        NOT NULL,
    "FK_Families"     BIGINT        NOT NULL,
    "ImageUrl"        VARCHAR(2000) NULL,
    "StorageKey"      VARCHAR(500)  NULL,
    "FileName"        VARCHAR(200)  NULL,
    "MimeType"        VARCHAR(100)  NULL,
    "FileSize"        BIGINT        NOT NULL,
    "SortOrder"       INTEGER       NOT NULL DEFAULT 0,
    "IsCoverImage"    BOOLEAN       NOT NULL DEFAULT FALSE,
    "CreatedBy"       VARCHAR(100)  NOT NULL DEFAULT 'system',
    "CreatedOn"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedBy"       VARCHAR(100)  NULL,
    "UpdatedOn"       TIMESTAMPTZ   NULL,
    "IsCancelled"     BOOLEAN       NOT NULL DEFAULT FALSE,
    "CancelledBy"     VARCHAR(100)  NULL,
    "CancelledOn"     TIMESTAMPTZ   NULL,
    CONSTRAINT "FK_MemoryImages_Memories"
        FOREIGN KEY ("FK_Memories") REFERENCES "Memories" ("ID_Memories") ON DELETE RESTRICT,
    CONSTRAINT "FK_MemoryImages_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "IX_MemoryImages_Memory"
    ON "MemoryImages" ("FK_Memories")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_MemoryImages_Family"
    ON "MemoryImages" ("FK_Families")
    WHERE "IsCancelled" = FALSE;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_MemoryImages_Memory_Cover_Active"
    ON "MemoryImages" ("FK_Memories")
    WHERE "IsCoverImage" = TRUE AND "IsCancelled" = FALSE;
