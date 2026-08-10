CREATE TABLE IF NOT EXISTS "Families" (
    "ID_Families"   BIGSERIAL PRIMARY KEY,
    "FamilyCode"    VARCHAR(50)  NOT NULL,
    "FamilyName"    VARCHAR(200) NOT NULL,
    "Description"   VARCHAR(1000) NULL,
    "CreatedBy"     VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"     VARCHAR(100) NULL,
    "UpdatedOn"     TIMESTAMPTZ  NULL,
    "IsCancelled"   BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"   VARCHAR(100) NULL,
    "CancelledOn"   TIMESTAMPTZ  NULL
);

ALTER TABLE "Families"
    ADD COLUMN IF NOT EXISTS "PhotoUrl" VARCHAR(2000) NULL;

ALTER TABLE "Families"
    ADD COLUMN IF NOT EXISTS "CoverUrl" VARCHAR(2000) NULL;
