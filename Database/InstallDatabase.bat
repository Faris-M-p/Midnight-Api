@echo off
setlocal

REM One-click fresh install. Double-click this file.
REM Credentials match appsettings.json DefaultConnection.

set HOST=localhost
set PORT=5432
set DB=midnight_family_tree
set USER=postgres
set PGPASSWORD=8816
set PSQL="C:\Program Files\PostgreSQL\18\bin\psql.exe"

cd /d "%~dp0"

echo ============================================
echo Midnight database install
echo Database: %DB%
echo ============================================

if not exist %PSQL% (
  echo psql not found at %PSQL%
  echo Install PostgreSQL or update PSQL path in this bat file.
  pause
  exit /b 1
)

echo Creating database "%DB%" if it does not exist...
%PSQL% -h %HOST% -p %PORT% -U %USER% -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = '%DB%'" | findstr /r "1" >nul
if errorlevel 1 (
  %PSQL% -h %HOST% -p %PORT% -U %USER% -d postgres -c "CREATE DATABASE \"%DB%\";"
  if errorlevel 1 (
    echo Create database failed.
    pause
    exit /b 1
  )
) else (
  echo Database "%DB%" already exists.
)

echo Installing schema into "%DB%"...
%PSQL% -h %HOST% -p %PORT% -U %USER% -d %DB% -v ON_ERROR_STOP=1 -f Database.sql
if errorlevel 1 (
  echo Install failed.
  pause
  exit /b 1
)

echo Install succeeded.
pause
endlocal
