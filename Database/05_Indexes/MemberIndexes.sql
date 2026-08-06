CREATE UNIQUE INDEX IF NOT EXISTS "IX_Families_FamilyCode"
    ON "Families" ("FamilyCode");

CREATE INDEX IF NOT EXISTS "IX_Members_FK_Families"
    ON "Members" ("FK_Families")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Members_FK_Members_Parent"
    ON "Members" ("FK_Members_Parent")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Members_FK_Members_Spouse"
    ON "Members" ("FK_Members_Spouse")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Members_IsRoot"
    ON "Members" ("FK_Families", "IsRoot")
    WHERE "IsCancelled" = FALSE AND "IsRoot" = TRUE;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAccounts_Username"
    ON "UserAccounts" ("Username");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAccounts_FK_Families"
    ON "UserAccounts" ("FK_Families");

CREATE INDEX IF NOT EXISTS "IX_MemberAddresses_FK_Members"
    ON "MemberAddresses" ("FK_Members")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_MemberImages_FK_Members"
    ON "MemberImages" ("FK_Members")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_MemberEvents_FK_Members"
    ON "MemberEvents" ("FK_Members")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_MemberNotes_FK_Members"
    ON "MemberNotes" ("FK_Members")
    WHERE "IsCancelled" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_MemberSocialLinks_FK_Members"
    ON "MemberSocialLinks" ("FK_Members")
    WHERE "IsCancelled" = FALSE;
