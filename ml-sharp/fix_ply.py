import numpy as np
from plyfile import PlyData, PlyElement
import os

INPUT_FILE = r"D:\AI_Project\ml-sharp\output\微信图片_20251224143232_27_4.ply" 
OUTPUT_FILE = "output/Solid_Debug_Model.ply"

def nuclear_fix():
    print(f"☢️ 启动修复模式: {INPUT_FILE}")
    if not os.path.exists(INPUT_FILE):
        print(f"❌ 找不到文件！")
        return

    plydata = PlyData.read(INPUT_FILE)
    v = plydata['vertex']
    count = len(v['x'])
    
    prop_names = [p.name for p in v.properties]
    
    new_data = np.zeros(count, dtype=[(name, 'f4') for name in prop_names])
    for name in prop_names:
        new_data[name] = v[name]

    print("🎨 正在强制将模型涂成实心...")
    if 'opacity' in prop_names:
        new_data['opacity'] = np.full(count, 10.0, dtype='f4')
    else:

    x, y, z = new_data['x'], new_data['y'], new_data['z']
    cx, cy, cz = np.mean(x), np.mean(y), np.mean(z)
    x -= cx; y -= cy; z -= cz # 归零
    
    dist = np.sqrt(x**2 + y**2 + z**2)
    max_dist = np.max(dist)
    scale_factor = 1.0 / max_dist if max_dist > 0 else 1.0
    x *= scale_factor; y *= scale_factor; z *= scale_factor 

    log_scale_adjustment = np.log(scale_factor)
    for s in ['scale_0', 'scale_1', 'scale_2']:
        if s in prop_names:
            new_data[s] += log_scale_adjustment

    os.makedirs(os.path.dirname(OUTPUT_FILE), exist_ok=True)
    PlyData([PlyElement.describe(new_data, 'vertex')]).write(OUTPUT_FILE)
    print(f"✅ 生成文件: Solid_Debug_Model.ply")
    print(f"👉 请把这个新文件拖入 Unity")

if __name__ == "__main__":
    nuclear_fix()
