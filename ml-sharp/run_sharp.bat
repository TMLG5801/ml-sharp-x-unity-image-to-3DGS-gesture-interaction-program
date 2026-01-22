@echo off
chcp 65001 >nul

:: --- 1. 设置模型缓存路径到 D 盘 ---
set TORCH_HOME=D:\AI_Project\cache
if not exist "D:\AI_Project\cache" mkdir "D:\AI_Project\cache"

:: --- 2. 切换到项目目录 ---
D:
cd D:\AI_Project\ml-sharp

:: --- 3. 激活环境并启动 ---
call D:\Miniconda3\Scripts\activate.bat D:\AI_Project\sharp-env

echo.
echo =================================================
echo  Sharp 3D 正在启动...
echo  第一次运行需要下载模型，请耐心等待！
echo =================================================
echo.

python app.py

pause
