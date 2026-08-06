@echo off
setlocal

REM Install a brand-new Midnight database schema.
REM Usage: InstallDatabase.bat [host] [port] [database] [username]
REM Password is read from PGPASSWORD env var or prompted by psql.

set HOST=%~1
if "%HOST%"=="" set HOST=localhost

set PORT=%~2
if "%PORT%"=="" set PORT=5432

set DB=%~3
if "%DB%"=="" set DB=midnight_family_tree_v2

set USER=%~4
if "%USER%"=="" set USER=postgres

cd /d "%~dp0"

echo Creating database "%DB%" if it does not exist...
psql -h %HOST% -p %PORT% -U %USER% -d postgres -tc "SELECT 1 FROM pg_database WHERE datname = '%DB%'" | findstr /r "1" >nul
if errorlevel 1 (
  psql -h %HOST% -p %PORT% -U %USER% -d postgres -c "CREATE DATABASE \"%DB%\";"
)

echo Installing schema into "%DB%"...
psql -h %HOST% -p %PORT% -U %USER% -d %DB% -v ON_ERROR_STOP=1 -f Database.sql
if errorlevel 1 (
  echo Install failed.
  exit /b 1
)

echo Install succeeded.
endlocal
