CREATE TABLE IF NOT EXISTS "MemberImages" (
    "ID_MemberImages" BIGSERIAL PRIMARY KEY,
    "FK_Members"      BIGINT        NOT NULL,
    "ImageUrl"        VARCHAR(2000) NOT NULL,
    "Caption"         VARCHAR(500)  NULL,
    "IsPrimary"       BOOLEAN       NOT NULL DEFAULT FALSE,
    "SortOrder"       INTEGER       NOT NULL DEFAULT 0,
    "CreatedBy"       VARCHAR(100)  NOT NULL DEFAULT 'system',
    "CreatedOn"       TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedBy"       VARCHAR(100)  NULL,
    "UpdatedOn"       TIMESTAMPTZ   NULL,
    "IsCancelled"     BOOLEAN       NOT NULL DEFAULT FALSE,
    "CancelledBy"     VARCHAR(100)  NULL,
    "CancelledOn"     TIMESTAMPTZ   NULL,
    CONSTRAINT "FK_MemberImages_Members"
        FOREIGN KEY ("FK_Members") REFERENCES "Members" ("ID_Members") ON DELETE RESTRICT
);
