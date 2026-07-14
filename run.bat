@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ============================================
echo  FarmManager - Local Dev Runner
echo ============================================
echo.

if not exist ".env" (
    echo Creating root .env from .env.example...
    copy /y ".env.example" ".env" >nul
)

if not exist "apps\api\.env" (
    echo Creating apps\api\.env from .env.example...
    copy /y "apps\api\.env.example" "apps\api\.env" >nul
)

echo [1/5] Starting Postgres (docker compose)...
docker compose up -d db
if errorlevel 1 (
    echo.
    echo Could not start Docker. Is Docker Desktop running?
    pause
    exit /b 1
)

echo Waiting for Postgres to become healthy...
:waitdb
set DBSTATUS=
for /f "tokens=*" %%s in ('docker inspect -f "{{.State.Health.Status}}" farmmanager-db-1 2^>nul') do set DBSTATUS=%%s
if not "!DBSTATUS!"=="healthy" (
    timeout /t 2 >nul
    goto waitdb
)
echo Postgres is healthy.
echo.

echo [2/5] Installing backend dependencies (only installs what's missing)...
call apps\api\.venv\Scripts\python.exe -m pip install -q -r apps\api\requirements.txt
echo.

echo [3/5] Applying database migrations...
pushd apps\api
call .venv\Scripts\python.exe -m alembic upgrade head
popd
echo.

echo [4/5] Seeding demo owner account...
pushd apps\api
call .venv\Scripts\python.exe -m app.core.seed_admin --username demo --display-name "Demo Owner" --password demo1234
popd
echo (If it said the account already exists, that's fine - it's already seeded.)
echo.

echo [5/5] Starting backend and frontend in new windows...
start "FarmManager API" cmd /k "cd /d %~dp0apps\api && .venv\Scripts\python.exe -m uvicorn app.main:app --reload --port 8000"
timeout /t 3 >nul
start "FarmManager Web" cmd /k "cd /d %~dp0apps\web && dotnet run --urls http://localhost:5083"

echo.
echo Waiting for the frontend to finish starting...
timeout /t 12 >nul
start "" "http://localhost:5083"

echo.
echo ============================================
echo  FarmManager is running
echo.
echo  Frontend: http://localhost:5083
echo  Backend:  http://localhost:8000
echo.
echo  Demo login:
echo    Username: demo
echo    Password: demo1234
echo.
echo  Close the two new console windows (or Ctrl+C in
echo  each) to stop the app. Postgres keeps running in
echo  Docker - stop it with: docker compose stop db
echo ============================================
pause
