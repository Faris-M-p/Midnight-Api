@echo off
setlocal

REM One-click patch. Double-click this file.
REM Uncomment the desired \i lines inside each folder Patch.sql first.
REM Credentials match appsettings.json DefaultConnection.

set HOST=localhost
set PORT=5432
set DB=midnight_family_tree
set USER=postgres
set PGPASSWORD=8816
set PSQL="C:\Program Files\PostgreSQL\18\bin\psql.exe"

cd /d "%~dp0"

echo ============================================
echo Midnight database patch
echo Database: %DB%
echo ============================================

if not exist %PSQL% (
  echo psql not found at %PSQL%
  echo Install PostgreSQL or update PSQL path in this bat file.
  pause
  exit /b 1
)

echo Patching database "%DB%"...
%PSQL% -h %HOST% -p %PORT% -U %USER% -d %DB% -v ON_ERROR_STOP=1 -f Database-Patch.sql
if errorlevel 1 (
  echo Patch failed.
  pause
  exit /b 1
)

echo Patch succeeded.
pause
endlocal
