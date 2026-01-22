using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement; // 必须引用：用于场景管理
using System.IO;
using System.Reflection;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;

public class AutoImporter
{
    static string plyName = "Auto_Model.ply";
    static string folderPath = "Assets/AutoImport";
    
    // 👇 指定主场景路径
    static string mainScenePath = "Assets/GSTestScene.unity";

    public static void Run()
    {
        if (EditorSceneManager.GetActiveScene().path != mainScenePath)
        {
            if (File.Exists(mainScenePath))
            {
                EditorSceneManager.OpenScene(mainScenePath);
                Debug.Log($"✅ [AutoImporter] 已切换至主场景: {mainScenePath}");
            }
            else
            {
                Debug.LogError($"❌ 未找到场景文件: {mainScenePath}，将继续在当前场景运行。");
            }
        }
        // =======================================================

        string plyPath = Path.Combine(folderPath, plyName);
        
        // 检查模型文件是否存在
        if (!File.Exists(plyPath)) 
        {
            Debug.LogWarning($"⚠️ 未找到生成的 PLY 文件: {plyPath}，仅打开场景。");
            return;
        }

        // 2. 导入资源
        AssetDatabase.ImportAsset(plyPath, ImportAssetOptions.ForceUpdate);
        GenerateAsset(plyPath);
        AssetDatabase.Refresh();

        // 3. 设置场景物体 (此时已经处于 GSTestScene 中)
        GameObject targetObj = SetupSceneObject(plyPath);

        // 4. 挂载控制脚本
        var gesture = targetObj.GetComponent<GestureController>();
        if (gesture == null) gesture = targetObj.AddComponent<GestureController>();

        // 设置默认参数
        gesture.mouseLookSensitivity = 0.5f;
        gesture.panSensitivity = 1.0f;
        gesture.fovSensitivity = 10.0f;
        gesture.playIntro = true; // 确保动画开启

        // 5. 清理旧脚本 (防止冲突)
        var oldScript1 = targetObj.GetComponent("RotateCard") as UnityEngine.Component;
        if (oldScript1 != null) UnityEngine.Object.DestroyImmediate(oldScript1);

        var oldScript2 = targetObj.GetComponent("AppearanceEffect") as UnityEngine.Component;
        if (oldScript2 != null) UnityEngine.Object.DestroyImmediate(oldScript2);

        // 6. 保存场景
        EditorUtility.SetDirty(targetObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // 7. 延迟启动 Play 模式
        EditorApplication.delayCall += () =>
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null) { EditorWindow.GetWindow(gameViewType).maximized = true; }
            EditorApplication.isPlaying = true;
        };
    }

    // --- 以下保持不变 ---

    static void GenerateAsset(string plyPath)
    {
        try
        {
            var creator = UnityEngine.ScriptableObject.CreateInstance<GaussianSplatAssetCreator>();
            var type = typeof(GaussianSplatAssetCreator);
            type.GetField("m_InputFile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(creator, plyPath);
            type.GetField("m_OutputFolder", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(creator, folderPath);
            EditorPrefs.SetInt("nesnausk.GaussianSplatting.CreatorQuality", 0);
            type.GetMethod("CreateAsset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(creator, null);
            UnityEngine.Object.DestroyImmediate(creator);
        }
        catch { }
    }

    static GameObject SetupSceneObject(string plyPath)
    {
        string assetName = Path.GetFileNameWithoutExtension(plyPath);
        string assetPath = $"{folderPath}/{assetName}.asset";
        
        // 在当前场景查找 Photo_Card
        GameObject targetObj = GameObject.Find("Photo_Card");
        if (targetObj == null)
        {
            targetObj = new GameObject("Photo_Card");
            targetObj.AddComponent<GaussianSplatRenderer>();
        }
        
        var renderer = targetObj.GetComponent<GaussianSplatRenderer>();
        var gsAsset = AssetDatabase.LoadAssetAtPath<GaussianSplatAsset>(assetPath);
        if (gsAsset != null) { renderer.m_Asset = gsAsset; EditorUtility.SetDirty(targetObj); }

        targetObj.transform.position = Vector3.zero;
        return targetObj;
    }
}