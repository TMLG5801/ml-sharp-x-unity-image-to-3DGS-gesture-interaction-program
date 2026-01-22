@echo off
setlocal

:: 1. 切换到脚本所在目录 (防止路径错误)
cd /d "%~dp0"

:: 2. 检查有没有安装过环境
if not exist "venv\Scripts\python.exe" (
    echo ==============================================
    echo [ERROR] 找不到虚拟环境！
    echo 请先运行 install_env.bat 进行安装。
    echo ==============================================
    pause
    exit /b
)

:: 3. 提示信息
echo [INFO] 正在启动 ML-Sharp x Unity...
echo [INFO] 使用环境: %~dp0venv\Scripts\python.exe

:: 4. 直接使用 venv 里的 Python 启动 (最稳妥的方式)
:: 注意：这里直接指定了路径，避免了 activate 失败的问题
venv\Scripts\python.exe ml-sharp/Launcher_Ultimate.py

:: 5. 如果程序崩溃，暂停查看报错
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] 程序异常退出，请检查上方报错信息。
    pause
)
