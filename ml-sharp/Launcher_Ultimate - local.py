import tkinter as tk
from tkinter import filedialog, messagebox, ttk
import os
import sys
import subprocess
import threading
import time
import json
import numpy as np
import datetime
import shutil #用于复制图片文件
from PIL import Image, ExifTags, ImageTk #ImageTk

# 尝试导入 plyfile
try:
    from plyfile import PlyData, PlyElement
except ImportError:
    messagebox.showerror("错误", "缺少 plyfile 库！请运行: pip install plyfile")
    sys.exit()

# ================= 配置中心 =================
UNITY_EXE_PATH = r"D:\unity\Editors\Editor\Unity.exe" 
UNITY_PROJECT_PATH = r"D:\AI_Project\Gaussian-URP"
SHARP_EXE_PATH = r"D:\AI_Project\sharp-env\Scripts\sharp.exe"
SHARP_ENV_ROOT = r"D:\AI_Project\sharp-env" 

HAND_CONTROL_SCRIPT = "hand_control.py" 
OUTPUT_DIR = "output"
UNITY_IMPORT_FOLDER = os.path.join(UNITY_PROJECT_PATH, "Assets", "AutoImport")

os.environ["TORCH_HOME"] = r"D:\AI_Project\cache"
# ==========================================================

unity_process = None
hand_process = None
image_cache = [] # 防止图片被垃圾回收

# 自动寻找 Python
def find_python_executable():
    path1 = os.path.join(SHARP_ENV_ROOT, "Scripts", "python.exe")
    if os.path.exists(path1): return path1
    path2 = os.path.join(SHARP_ENV_ROOT, "python.exe")
    if os.path.exists(path2): return path2
    return None

# --- 读取 EXIF ---
def get_focal_length(image_path):
    default_focal = 30.0
    try:
        img = Image.open(image_path)
        exif_data = img._getexif()
        if not exif_data: return default_focal
        focal_35mm = exif_data.get(0xA405)
        if focal_35mm: return float(focal_35mm)
        focal_val = exif_data.get(37386)
        if focal_val:
            if isinstance(focal_val, tuple):
                return float(focal_val[0]) / float(focal_val[1]) if focal_val[1] != 0 else default_focal
            return float(focal_val)
        return default_focal
    except: return default_focal

def write_camera_config(focal_mm):
    config = { "focal_length_mm": focal_mm }
    if not os.path.exists(UNITY_IMPORT_FOLDER): os.makedirs(UNITY_IMPORT_FOLDER)
    json_path = os.path.join(UNITY_IMPORT_FOLDER, "camera_info.json")
    with open(json_path, 'w') as f: json.dump(config, f)

# --- 转换 PLY ---
def smart_convert_ply(input_file, output_file):
    print(f"🧠 [智能转换] 处理中: {input_file}")
    if not os.path.exists(input_file): raise Exception(f"源文件丢失: {input_file}")

    plydata = PlyData.read(input_file)
    v = plydata['vertex']
    
    name_map = {"alpha": "opacity", "red": "f_dc_0", "green": "f_dc_1", "blue": "f_dc_2", "nx": "nx", "ny": "ny", "nz": "nz"}
    original_names = [p.name for p in v.properties]
    new_dtype = []
    for name in original_names:
        new_name = name_map.get(name, name)
        new_dtype.append((new_name, 'f4'))
    
    new_data = np.zeros(len(v['x']), dtype=new_dtype)
    for name in original_names:
        new_data[name_map.get(name, name)] = v[name]

    if 'opacity' in new_data.dtype.names:
        op_vals = new_data['opacity']
        min_val, max_val = np.min(op_vals), np.max(op_vals)
        if min_val >= -0.01 and max_val <= 1.01:
            op_vals = np.clip(op_vals, 1e-6, 0.999999)
            new_data['opacity'] = np.log(op_vals / (1 - op_vals))

    x, y, z = new_data['x'], new_data['y'], new_data['z']
    x -= np.mean(x); y -= np.mean(y); z -= np.mean(z)
    
    max_dist = np.max(np.sqrt(x**2 + y**2 + z**2))
    if max_dist > 100.0:
        scale = 10.0 / max_dist
        x *= scale; y *= scale; z *= scale
        log_scale = np.log(scale)
        for s in ['scale_0', 'scale_1', 'scale_2']:
            if s in new_data.dtype.names: new_data[s] += log_scale

    os.makedirs(os.path.dirname(output_file), exist_ok=True)
    PlyData([PlyElement.describe(new_data, 'vertex')]).write(output_file)
    print(f"💎 [转换完成] -> {output_file}")

# --- AI 推理 ---
def run_ml_sharp_realtime(image_path, status_callback):
    if not os.path.exists(OUTPUT_DIR): os.makedirs(OUTPUT_DIR)
    sharp_exe = SHARP_EXE_PATH if os.path.exists(SHARP_EXE_PATH) else "sharp"
    cmd = [sharp_exe, "predict", "-i", image_path, "-o", OUTPUT_DIR]
    process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1, universal_newlines=True)
    for line in process.stdout:
        line = line.strip()
        if line:
            print(f"[SHARP] {line}")
            if "Loading" in line: status_callback("正在加载权重...")
            if "Generating" in line: status_callback("正在生成...")
    process.wait()
    import glob
    files = glob.glob(f'{OUTPUT_DIR}/*.ply')
    if not files: return None
    
    # 获取最新生成的 ply 文件
    latest_ply = max(files, key=os.path.getmtime)
    
    # ✅ 新功能：备份源图片到 output 目录，用于历史预览
    try:
        ply_name = os.path.basename(latest_ply)
        img_ext = os.path.splitext(image_path)[1]
        # 对应的图片名字，例如 model.ply -> model.jpg
        backup_img_name = os.path.splitext(ply_name)[0] + img_ext
        backup_img_path = os.path.join(OUTPUT_DIR, backup_img_name)
        shutil.copy2(image_path, backup_img_path)
        print(f"🖼️ [图片备份] 已保存预览图: {backup_img_path}")
    except Exception as e:
        print(f"⚠️ 图片备份失败: {e}")

    return latest_ply

# --- 🎮 Unity 控制 ---
def launch_unity():
    global unity_process
    print("🚀 启动 Unity...")
    cmd = [UNITY_EXE_PATH, "-projectPath", UNITY_PROJECT_PATH, "-executeMethod", "AutoImporter.Run"]
    unity_process = subprocess.Popen(cmd)
    t = threading.Thread(target=monitor_unity_exit)
    t.start()

def monitor_unity_exit():
    global unity_process, hand_process
    if unity_process:
        unity_process.wait()
        print("🛑 Unity 已退出")
        if hand_process: hand_process.terminate()
        os._exit(0)

def launch_hand_control():
    global hand_process
    script_path = os.path.abspath(HAND_CONTROL_SCRIPT)
    if not os.path.exists(script_path):
        messagebox.showerror("错误", f"找不到 {script_path}")
        return
    python_exe = find_python_executable()
    if not python_exe:
        messagebox.showerror("环境错误", f"找不到 python.exe，请检查路径设置。")
        return

    cmd = [python_exe, script_path]
    if hand_process is None or hand_process.poll() is not None:
        try:
            hand_process = subprocess.Popen(cmd, cwd=os.path.dirname(script_path))
            btn_hand.config(text="✋ 手势控制运行中 (点击重启)", bg="green")
        except Exception as e:
            messagebox.showerror("启动失败", f"错误详情:\n{e}")
    else:
        hand_process.terminate()
        hand_process = subprocess.Popen(cmd, cwd=os.path.dirname(script_path))

# --- 📁 ✅ 全新升级：带预览图的历史记录选择 ---
def select_history_file():
    import glob
    ply_files = glob.glob(f'{OUTPUT_DIR}/*.ply')
    if not ply_files: return None
    ply_files.sort(key=os.path.getmtime, reverse=True)
    
    selection = None
    image_cache.clear() # 清理旧缓存

    top = tk.Toplevel(window)
    top.title("历史记录库")
    top.geometry("650x500")
    top.configure(bg="#333")

    # 创建滚动区域
    canvas = tk.Canvas(top, bg="#333", highlightthickness=0)
    scrollbar = ttk.Scrollbar(top, orient="vertical", command=canvas.yview)
    scrollable_frame = tk.Frame(canvas, bg="#333")

    scrollable_frame.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))
    canvas.create_window((0, 0), window=scrollable_frame, anchor="nw")
    canvas.configure(yscrollcommand=scrollbar.set)

    canvas.pack(side="left", fill="both", expand=True, padx=10, pady=10)
    scrollbar.pack(side="right", fill="y", pady=10)

    def on_select(ply_path):
        nonlocal selection
        selection = ply_path
        top.destroy()

    # 寻找对应的图片
    def find_partner_image(ply_path):
        base_path = os.path.splitext(ply_path)[0]
        for ext in ['.jpg', '.png', '.jpeg', '.heic']:
            img_path = base_path + ext
            if os.path.exists(img_path): return img_path
        return None

    # 生成列表项
    for i, ply_path in enumerate(ply_files):
        fname = os.path.basename(ply_path)
        ts = os.path.getmtime(ply_path)
        dt = datetime.datetime.fromtimestamp(ts).strftime('%Y-%m-%d %H:%M')
        
        item_frame = tk.Frame(scrollable_frame, bg="#444", pady=5, padx=5)
        item_frame.pack(fill="x", pady=2)

        # 处理图片预览
        img_path = find_partner_image(ply_path)
        tk_img = None
        if img_path:
            try:
                pil_img = Image.open(img_path)
                pil_img.thumbnail((80, 80)) # 生成缩略图
                tk_img = ImageTk.PhotoImage(pil_img)
                image_cache.append(tk_img) # 重要：防止被回收
            except: pass
        
        if tk_img:
            lbl_img = tk.Label(item_frame, image=tk_img, bg="#444")
            lbl_img.pack(side="left", padx=(0, 10))
        else:
            # 没有图片的占位符
            lbl_noimg = tk.Label(item_frame, text="无预览", bg="#666", fg="#ccc", width=10, height=4)
            lbl_noimg.pack(side="left", padx=(0, 10))

        info_text = f"📄 {fname}\n🕒 {dt}"
        lbl_text = tk.Label(item_frame, text=info_text, bg="#444", fg="white", font=("Consolas", 11), justify="left")
        lbl_text.pack(side="left", fill="y")

        btn_load = tk.Button(item_frame, text="加载", bg="#00AACC", fg="white", relief="flat", 
                             command=lambda p=ply_path: on_select(p))
        btn_load.pack(side="right", padx=10, pady=10)

    window.wait_window(top)
    return selection

# --- 主逻辑 ---
def process_logic(img_path, load_history=False):
    try:
        if not load_history:
            focal = get_focal_length(img_path)
            write_camera_config(focal)
            status_lbl.config(text="AI 生成中...")
            progress['value'] = 20
            # run_ml_sharp_realtime 现在会负责备份图片
            raw_ply = run_ml_sharp_realtime(img_path, lambda x: status_lbl.config(text=x))
            if not raw_ply: raise Exception("生成失败")
        else:
            raw_ply = select_history_file()
            if not raw_ply: 
                status_lbl.config(text="取消选择")
                return
            status_lbl.config(text=f"加载历史: {os.path.basename(raw_ply)}")

        progress['value'] = 60
        status_lbl.config(text="转换格式中...")
        if not os.path.exists(UNITY_IMPORT_FOLDER): os.makedirs(UNITY_IMPORT_FOLDER)
        final_ply = os.path.join(UNITY_IMPORT_FOLDER, "Auto_Model.ply")
        smart_convert_ply(raw_ply, final_ply)
        
        progress['value'] = 100
        status_lbl.config(text="启动 Unity...")
        launch_unity()
        
    except Exception as e:
        messagebox.showerror("错误", str(e))
        status_lbl.config(text="出错")

def on_generate_click():
    img = filedialog.askopenfilename(filetypes=[("Images", "*.jpg *.png *.jpeg *.heic")])
    if img: threading.Thread(target=process_logic, args=(img, False)).start()

def on_history_click():
    # 必须在主线程调用 Toplevel
    threading.Thread(target=process_logic, args=(None, True)).start()

# --- GUI ---
window = tk.Tk(); window.title("启动器 v8.0"); window.geometry("600x520"); window.configure(bg="#222")
tk.Label(window, text="ML-Sharp x Unity", font=("Segoe UI", 20, "bold"), bg="#222", fg="#00AACC").pack(pady=(30, 20))
frame = tk.Frame(window, bg="#222"); frame.pack(pady=10)
btn_gen = tk.Button(frame, text="✨ 选择图片生成", font=("Segoe UI", 12), width=20, command=on_generate_click, bg="#00AACC", fg="white", relief="flat", padx=10, pady=5); btn_gen.grid(row=0, column=0, padx=10)
btn_hist = tk.Button(frame, text="📂 历史记录库", font=("Segoe UI", 12), width=20, command=on_history_click, bg="#444", fg="white", relief="flat", padx=10, pady=5); btn_hist.grid(row=0, column=1, padx=10)
btn_hand = tk.Button(window, text="✋ 启动手势控制", font=("Segoe UI", 12), width=45, command=launch_hand_control, bg="#555", fg="white", relief="flat", pady=5); btn_hand.pack(pady=20)
status_lbl = tk.Label(window, text="System Ready", bg="#222", fg="#888", font=("Consolas", 10)); status_lbl.pack(pady=5)
progress = ttk.Progressbar(window, orient="horizontal", length=500, mode="determinate"); progress.pack(pady=10)
tk.Label(window, text="💡 提示: 关闭 Unity 窗口后，此程序会自动退出", bg="#222", fg="#555").pack(side="bottom", pady=20)

if __name__ == "__main__": window.mainloop()