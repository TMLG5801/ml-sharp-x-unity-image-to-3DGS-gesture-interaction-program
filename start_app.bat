@echo off
chcp 65001 >nul
title [启动器] ML-Sharp x Unity

if not exist "venv" (
    cls
    echo ========================================================
    echo [ERROR] 未找到运行环境！
    echo ========================================================
    echo.
    echo 请先双击运行 "install_env.bat" 进行安装。
    echo.
    pause
    exit
)

echo 正在启动程序...
call venv\Scripts\activate.bat
:: 启动你的启动器脚本
python ml-sharp/Launcher_Ultimate.py

pause