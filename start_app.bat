@echo off
setlocal

:: Switch to script directory
cd /d "%~dp0"

echo [INFO] Checking environment...

:: Check if venv exists
if not exist "venv\Scripts\python.exe" (
    echo.
    echo [ERROR] Virtual environment not found!
    echo Path: %~dp0venv\Scripts\python.exe
    echo.
    echo Please run 'install_env.bat' first to install dependencies.
    echo.
    pause
    exit /b
)

echo [INFO] Launching ML-Sharp x Unity...

:: Run the script using the venv python directly
"venv\Scripts\python.exe" "ml-sharp/Launcher_Ultimate.py"

:: Check for errors
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] The program exited with an error.
    pause
)