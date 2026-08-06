-- MidnightApi full database install script
-- Run with: psql -h HOST -U USER -d DATABASE -f Database.sql
-- Or use InstallDatabase.bat

\echo '=== 01_Tables ==='
\i 01_Tables/Families.sql
\i 01_Tables/Members.sql
\i 01_Tables/MemberAddresses.sql
\i 01_Tables/MemberImages.sql
\i 01_Tables/MemberEvents.sql
\i 01_Tables/MemberNotes.sql
\i 01_Tables/MemberSocialLinks.sql
\i 01_Tables/UserAccounts.sql

\echo '=== 02_Functions ==='
\i 02_Functions/FnGetGeneration.sql
\i 02_Functions/FnGetRelationship.sql

\echo '=== 03_Types ==='
\i 03_Types/EventType.sql
\i 03_Types/NotificationType.sql

\echo '=== 04_Views ==='
\i 04_Views/ViewDashboard.sql
\i 04_Views/ViewMembers.sql

\echo '=== 05_Indexes ==='
\i 05_Indexes/MemberIndexes.sql

\echo '=== 06_Triggers ==='
\i 06_Triggers/MemberAudit.sql

\echo '=== 07_Procedures ==='
\i 07_Procedures/Account/ProAccountSelect.sql
\i 07_Procedures/Account/ProAccountLogin.sql
\i 07_Procedures/Account/ProAccountRegister.sql
\i 07_Procedures/Account/ProAccountUpdate.sql
\i 07_Procedures/Account/ProAccountExistsByUsername.sql

\i 07_Procedures/Family/ProFamilySelect.sql
\i 07_Procedures/Family/ProFamilyInsert.sql
\i 07_Procedures/Family/ProFamilyUpdate.sql
\i 07_Procedures/Family/ProFamilyExistsByCode.sql

\i 07_Procedures/Member/ProMemberList.sql
\i 07_Procedures/Member/ProMemberTree.sql
\i 07_Procedures/Member/ProMemberSelect.sql
\i 07_Procedures/Member/ProMemberInsert.sql
\i 07_Procedures/Member/ProMemberUpdate.sql
\i 07_Procedures/Member/ProMemberDelete.sql
\i 07_Procedures/Member/ProMemberMapSpouse.sql
\i 07_Procedures/Member/ProMemberDashboard.sql
\i 07_Procedures/Member/ProMemberTimeline.sql
\i 07_Procedures/Member/ProMemberExistsInFamily.sql
\i 07_Procedures/Member/ProMemberHasRoot.sql
\i 07_Procedures/Member/ProMemberGetParentId.sql
\i 07_Procedures/Member/ProMemberGetRelation.sql

\echo '=== 08_SeedData ==='
\i 08_SeedData/DefaultSettings.sql
\i 08_SeedData/DefaultPermissions.sql

\echo '=== Database install complete ==='
