import sys
import os
import importlib

# 1. 设置路径
current_dir = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, current_dir)

print(f"[INFO] 正在尝试从 {current_dir} 启动 Sharp...")

try:
    # 尝试直接导入 sharp.cli 包
    try:
        import sharp.cli
    except ImportError:
        # 如果 sharp 还没在 path 里，显式把 sharp 目录加进去
        pass

    entry_func = None
    
    # --- 侦测逻辑 ---
    # 扫描 sharp/cli 文件夹
    cli_dir = os.path.join(current_dir, 'sharp', 'cli')
    if os.path.exists(cli_dir):
        # 常见的二级入口
        sub_modules = ['predict', 'main', 'run', 'console']
        for sub in sub_modules:
            try:
                module_name = f"sharp.cli.{sub}"
                mod = importlib.import_module(module_name)
                # 找 predict_cli 或 main
                candidates = ['predict_cli', 'main', 'cli', 'run']
                for func_name in candidates:
                    if hasattr(mod, func_name):
                        entry_func = getattr(mod, func_name)
                        print(f"[SUCCESS] 锁定入口函数: {module_name}.{func_name}")
                        break
                if entry_func: break
            except ImportError:
                continue

    # 4. 执行找到的函数
    if entry_func:
        print("[INFO] 正在启动引擎...")
        
        if "predict" in sys.argv:
            sys.argv.remove("predict")
            
        sys.exit(entry_func())
    else:
        # 最后的保底（虽然根据日志你应该走不到这里了）
        print("\n[ERROR] 居然没找到入口？这不科学。")
        sys.exit(1)

except Exception as e:
    print(f"\n[CRITICAL ERROR] 运行出错: {e}")
    import traceback
    traceback.print_exc()
    sys.exit(1)