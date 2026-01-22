import os
import sys
import subprocess
import tkinter as tk
from tkinter import filedialog
from pathlib import Path
import time

# --- 配置 ---
OUTPUT_DIR = "output"

# 设置缓存目录到 D 盘
if "TORCH_HOME" not in os.environ:
    os.environ["TORCH_HOME"] = "D:\\AI_Project\\cache"

def select_file():
    """ 弹出文件选择框 """
    try:
        root = tk.Tk()
        root.withdraw() # 隐藏主窗口
        path = filedialog.askopenfilename(
            title="选择图片生成 3D 模型",
            filetypes=[("Images", "*.jpg *.png *.heic *.jpeg")]
        )
        root.destroy()
        return path
    except Exception as e:
        print(f"❌ 选择文件出错: {e}")
        return None

def generate_3d(image_path):
    """ 调用 sharp.exe 生成 PLY 模型 """
    name = Path(image_path).stem
    # 确保输出目录存在
    if not os.path.exists(OUTPUT_DIR):
        os.makedirs(OUTPUT_DIR)
        
    ply_path = os.path.join(OUTPUT_DIR, f"{name}.ply")
    
    # 1. 检查是否有缓存（如果文件存在且大于 1KB，直接跳过生成）
    if os.path.exists(ply_path) and os.path.getsize(ply_path) > 1024:
        print(f"⚡ 发现已有缓存，跳过生成: {name}")
        return ply_path 
            
    print(f"🔨 AI 正在全力生成: {name}")
    print("⏳ (笔记本显卡请耐心等待约 1-3 分钟，期间请勿操作其他软件)...")

    # 寻找 sharp.exe 的位置
    sharp_exe = os.path.join(sys.prefix, "Scripts", "sharp.exe")
    if not os.path.exists(sharp_exe): 
        sharp_exe = "sharp" # 尝试直接调用
        
    cmd = [sharp_exe, "predict", "-i", image_path, "-o", OUTPUT_DIR]
    
    try:
        # 调用子进程执行生成，check=True 会在出错时抛出异常
        subprocess.run(cmd, check=True)
        
        # 寻找最新生成的 ply 文件
        import glob
        files = glob.glob(f'{OUTPUT_DIR}/*.ply')
        if files:
            return max(files, key=os.path.getmtime)
        return None
    except subprocess.CalledProcessError as e:
        print(f"❌ 生成失败 (显存不足或模型错误): {e}")
        return None
    except Exception as e:
        print(f"❌ 未知错误: {e}")
        return None

def main():
    print("="*50)
    print("🚀 3D 模型生成器 (Unity 专用纯净版) 已启动")
    print("💻 当前模式：只生成文件，不消耗显存预览")
    print("="*50)

    while True:
        print("\n📂 请选择一张图片 (取消选择将退出程序)...")
        image_path = select_file()
        
        if not image_path:
            print("👋 程序退出")
            break
            
        start_time = time.time()
        ply_file = generate_3d(image_path)
        
        if ply_file:
            print("\n" + "="*50)
            print(f"✅ [生成成功] 耗时: {time.time() - start_time:.1f}秒")
            print(f"📂 文件路径: {ply_file}")
            print(f"👉 现在！请把这个 .ply 文件拖入 Unity 的 Project 窗口")
            print("="*50)
        else:
            print("\n❌ 生成失败，请检查上方报错信息")
        
        # 简单防抖，防止误触
        time.sleep(1)

if __name__ == "__main__":
    main()
