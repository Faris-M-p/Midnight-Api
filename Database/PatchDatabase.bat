@echo off
setlocal

REM Apply Patch.sql scripts only.
REM Uncomment the desired \i lines inside each folder Patch.sql first.
REM Usage: PatchDatabase.bat [host] [port] [database] [username]

set HOST=%~1
if "%HOST%"=="" set HOST=localhost

set PORT=%~2
if "%PORT%"=="" set PORT=5432

set DB=%~3
if "%DB%"=="" set DB=midnight_family_tree_v2

set USER=%~4
if "%USER%"=="" set USER=postgres

cd /d "%~dp0"

echo Patching database "%DB%"...
psql -h %HOST% -p %PORT% -U %USER% -d %DB% -v ON_ERROR_STOP=1 -f Database-Patch.sql
if errorlevel 1 (
  echo Patch failed.
  exit /b 1
)

echo Patch succeeded.
endlocal
