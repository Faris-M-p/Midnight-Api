-- Idempotent cleanup for pre-MemoryImages memory media schema.
-- Safe to run on fresh or already-migrated databases.

ALTER TABLE IF EXISTS "Memories"
    DROP CONSTRAINT IF EXISTS "FK_Memories_CoverMedia";

ALTER TABLE IF EXISTS "Memories"
    DROP COLUMN IF EXISTS "FK_CoverMedia";

DROP TABLE IF EXISTS "MemoryMedia";
DROP TABLE IF EXISTS "Media";
