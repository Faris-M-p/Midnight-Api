-- MidnightApi patch script
-- Uncomment entries inside each Patch.sql before running.
-- Run with: psql -h HOST -U USER -d DATABASE -f Database-Patch.sql
-- Or use PatchDatabase.bat

\echo '=== Patch 01_Tables ==='
\ir 01_Tables/Patch.sql

\echo '=== Patch 02_Functions ==='
\ir 02_Functions/Patch.sql

\echo '=== Patch 03_Types ==='
\ir 03_Types/Patch.sql

\echo '=== Patch 04_Views ==='
\ir 04_Views/Patch.sql

\echo '=== Patch 05_Indexes ==='
\ir 05_Indexes/Patch.sql

\echo '=== Patch 06_Triggers ==='
\ir 06_Triggers/Patch.sql

\echo '=== Patch 07_Procedures ==='
\ir 07_Procedures/DropLegacyFunctions.sql
\ir 07_Procedures/Account/Patch.sql
\ir 07_Procedures/Family/Patch.sql
\ir 07_Procedures/Member/Patch.sql
\ir 07_Procedures/Event/Patch.sql
\ir 07_Procedures/Story/Patch.sql
\ir 07_Procedures/Notification/Patch.sql
\ir 07_Procedures/AccessToken/Patch.sql

\echo '=== Patch 08_SeedData ==='
\ir 08_SeedData/Patch.sql

\echo '=== Database patch complete ==='
