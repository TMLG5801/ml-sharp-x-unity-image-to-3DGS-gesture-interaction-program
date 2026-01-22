using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Reflection;
using GaussianSplatting.Editor;
using GaussianSplatting.Runtime;

public class AutoImporter
{
    static string plyName = "Auto_Model.ply";
    static string folderPath = "Assets/AutoImport";

    public static void Run()
    {
        string plyPath = Path.Combine(folderPath, plyName);
        if (!File.Exists(plyPath)) return;

        // 1. 导入资源
        AssetDatabase.ImportAsset(plyPath, ImportAssetOptions.ForceUpdate);
        GenerateAsset(plyPath);
        AssetDatabase.Refresh();

        // 2. 设置场景物体
        GameObject targetObj = SetupSceneObject(plyPath);

        // 3. 挂载控制脚本
        var gesture = targetObj.GetComponent<GestureController>();
        if (gesture == null) gesture = targetObj.AddComponent<GestureController>();

        // 设置默认参数
        gesture.mouseLookSensitivity = 0.5f;
        gesture.panSensitivity = 1.0f;
        gesture.fovSensitivity = 10.0f;
        gesture.playIntro = true; // 确保动画开启

        // 4. 清理旧脚本 (包括之前的 AppearanceEffect)
        var oldScript1 = targetObj.GetComponent("RotateCard") as UnityEngine.Component;
        if (oldScript1 != null) UnityEngine.Object.DestroyImmediate(oldScript1);

        // ⚠️ 关键：清理掉 AppearanceEffect，防止冲突
        var oldScript2 = targetObj.GetComponent("AppearanceEffect") as UnityEngine.Component;
        if (oldScript2 != null) UnityEngine.Object.DestroyImmediate(oldScript2);

        // 5. 保存场景
        EditorUtility.SetDirty(targetObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        // 6. 延迟启动
        EditorApplication.delayCall += () =>
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null) { EditorWindow.GetWindow(gameViewType).maximized = true; }
            EditorApplication.isPlaying = true;
        };
    }

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