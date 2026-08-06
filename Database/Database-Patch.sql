-- MidnightApi patch script
-- Uncomment entries inside each Patch.sql before running.
-- Run with: psql -h HOST -U USER -d DATABASE -f Database-Patch.sql
-- Or use PatchDatabase.bat

\echo '=== Patch 01_Tables ==='
\i 01_Tables/Patch.sql

\echo '=== Patch 02_Functions ==='
\i 02_Functions/Patch.sql

\echo '=== Patch 03_Types ==='
\i 03_Types/Patch.sql

\echo '=== Patch 04_Views ==='
\i 04_Views/Patch.sql

\echo '=== Patch 05_Indexes ==='
\i 05_Indexes/Patch.sql

\echo '=== Patch 06_Triggers ==='
\i 06_Triggers/Patch.sql

\echo '=== Patch 07_Procedures ==='
\i 07_Procedures/Account/Patch.sql
\i 07_Procedures/Family/Patch.sql
\i 07_Procedures/Member/Patch.sql
\i 07_Procedures/Event/Patch.sql
\i 07_Procedures/Story/Patch.sql
\i 07_Procedures/Notification/Patch.sql
\i 07_Procedures/AccessToken/Patch.sql

\echo '=== Patch 08_SeedData ==='
\i 08_SeedData/Patch.sql

\echo '=== Database patch complete ==='
