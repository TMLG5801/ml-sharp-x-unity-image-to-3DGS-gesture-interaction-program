@echo off
setlocal

cd /d "%~dp0"
echo [INFO] Project Root: %CD%

:: 1. Check Python
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python 3.10 is not installed or not in PATH!
    pause
    exit /b
)

:: 2. Create Venv
if not exist "venv" (
    echo [INFO] Creating virtual environment...
    python -m venv venv
) else (
    echo [INFO] venv already exists.
)

:: 3. Activate Venv
call venv\Scripts\activate

:: 4. Upgrade pip
python -m pip install --upgrade pip

echo [INFO] Installing PyTorch with CUDA support...
pip install torch==2.5.1 torchvision==0.20.1 --index-url https://download.pytorch.org/whl/cu124

:: 5. Install other dependencies
echo [INFO] Installing other dependencies...
pip install -r requirements.txt

echo.
echo ===================================================
echo [SUCCESS] Environment Installed Successfully!
echo [INFO] You can now run 'start_app.bat'
echo ===================================================
pause