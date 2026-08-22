CREATE TABLE IF NOT EXISTS "EventMembers" (
    "ID_EventMembers" BIGSERIAL PRIMARY KEY,
    "FK_Events"       BIGINT       NOT NULL,
    "FK_Members"      BIGINT       NOT NULL,
    "FK_Families"     BIGINT       NOT NULL,
    "CreatedBy"       VARCHAR(100) NOT NULL DEFAULT 'system',
    "CreatedOn"       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedBy"       VARCHAR(100) NULL,
    "UpdatedOn"       TIMESTAMPTZ  NULL,
    "IsCancelled"     BOOLEAN      NOT NULL DEFAULT FALSE,
    "CancelledBy"     VARCHAR(100) NULL,
    "CancelledOn"     TIMESTAMPTZ  NULL,
    CONSTRAINT "FK_EventMembers_Events"
        FOREIGN KEY ("FK_Events") REFERENCES "Events" ("ID_Events") ON DELETE RESTRICT,
    CONSTRAINT "FK_EventMembers_Members"
        FOREIGN KEY ("FK_Members") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT,
    CONSTRAINT "FK_EventMembers_Families"
        FOREIGN KEY ("FK_Families") REFERENCES "Families" ("ID_Families") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_EventMembers_Event_Member_Active"
    ON "EventMembers" ("FK_Events", "FK_Members")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_EventMembers_Event"
    ON "EventMembers" ("FK_Events")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_EventMembers_Member"
    ON "EventMembers" ("FK_Members")
    WHERE "IsCancelled" = FALSE;
