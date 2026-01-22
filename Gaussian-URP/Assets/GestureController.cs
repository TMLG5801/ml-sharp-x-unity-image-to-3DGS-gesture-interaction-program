using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class GestureController : MonoBehaviour
{
    public enum AppMode { Edit, View }

    [Header("✨ 入场动画")]
    public bool playIntro = true;
    public float introDuration = 1.2f;
    public AnimationCurve introCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.1f), new Keyframe(1, 1));

    [Header("🛠️ 编辑模式 (UI控制)")]
    [Range(0.1f, 10.0f)] public float manualDistance = 2.5f;
    [Range(1f, 120f)] public float fieldOfView = 10.0f;
    [Range(0.1f, 5.0f)] public float modelScale = 1.0f;

    [Header("🖱️ 编辑模式灵敏度")]
    [Tooltip("左键旋转的快慢")]
    [Range(0.05f, 2.0f)] public float editMouseSensitivity = 0.2f;
    [Tooltip("右键平移的快慢")]
    [Range(0.1f, 5.0f)] public float editPanSensitivity = 1.0f; // ✅ 平移灵敏度

    [Header("👁️ 查看模式 (飞行+手势)")]
    public bool isViewMode = false;
    [Range(0.1f, 10.0f)] public float flySpeed = 1.0f;
    [Range(0.1f, 5.0f)] public float mouseLookSensitivity = 0.5f;

    [Header("👋 手势灵敏度")]
    [Range(1f, 20f)] public float smoothSpeed = 5.0f;
    [Range(0.1f, 5f)] public float panSensitivity = 1.0f;
    [Range(1f, 50f)] public float fovSensitivity = 10.0f;

    [Header("🛠️ 状态显示")]
    public string lastReceived = "等待信号...";

    [Header("朝向设置")]
    public bool fixRotation180 = false;
    public bool mirrorX = false;
    public bool flipY = true;

    // 内部变量
    private Thread receiveThread;
    private UdpClient client;
    private bool isRunning = true;
    private int udpPort = 5052;

    private float targetRotY = 0f;
    private float targetRotX = 0f;
    private Vector2 targetPan = Vector2.zero;
    private float targetFOV;

    private Vector3 lastMousePos;
    private float camRotX = 0;
    private float camRotY = 0;

    // 动画相关
    private float introTimer = 0f;
    private float currentIntroScale = 1.0f;

    private bool showUI = true;
    private Rect windowRect = new Rect(20, 20, 350, 850); // 面板加长

    void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        if (Camera.main != null) Camera.main.nearClipPlane = 0.01f;

        targetFOV = fieldOfView;
        ResetTransform();

        if (playIntro)
        {
            introTimer = 0f;
            currentIntroScale = 0f;
        }
    }

    void RecordCameraRotation()
    {
        if (Camera.main != null)
        {
            camRotX = Camera.main.transform.eulerAngles.y;
            camRotY = Camera.main.transform.eulerAngles.x;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) showUI = !showUI;

        // 入场动画处理
        if (introTimer < introDuration)
        {
            introTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(introTimer / introDuration);
            currentIntroScale = introCurve.Evaluate(progress);
        }
        else
        {
            currentIntroScale = 1.0f;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        if (!isViewMode) targetFOV = fieldOfView;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);

        // 模式分流
        if (isViewMode) HandleViewMode(cam);
        else HandleEditMode(cam);

        ApplyModelTransform();
    }

    void HandleViewMode(Camera cam)
    {
        // 查看模式：右键旋转视角 (WASD方向)
        if (Input.GetMouseButton(1))
        {
            camRotX += Input.GetAxis("Mouse X") * mouseLookSensitivity;
            camRotY -= Input.GetAxis("Mouse Y") * mouseLookSensitivity;
            camRotY = Mathf.Clamp(camRotY, -90, 90);
            cam.transform.rotation = Quaternion.Euler(camRotY, camRotX, 0);
        }

        float speed = flySpeed * Time.deltaTime;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += cam.transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= cam.transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= cam.transform.right;
        if (Input.GetKey(KeyCode.D)) move += cam.transform.right;

        if (Input.GetKey(KeyCode.LeftShift)) move += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

        cam.transform.position += move * speed;
    }

    void HandleEditMode(Camera cam)
    {
        Vector3 targetPos = new Vector3(0, 0, -manualDistance);
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * smoothSpeed);
        cam.transform.LookAt(Vector3.zero);
        HandleMouseOrbitInEdit();
    }

    // ✅ 这里是修改的核心：彻底分离左键和右键逻辑
    void HandleMouseOrbitInEdit()
    {
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y;
        bool isMouseOverUI = showUI && windowRect.Contains(mousePos);

        // 按下瞬间记录坐标，防止瞬移
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && !isMouseOverUI)
            lastMousePos = Input.mousePosition;

        if (!isMouseOverUI)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;

            // 🖱️ 左键按下：只负责旋转
            if (Input.GetMouseButton(0))
            {
                targetRotY -= delta.x * editMouseSensitivity;
                targetRotX += delta.y * editMouseSensitivity;
                targetRotX = Mathf.Clamp(targetRotX, -90f, 90f);
            }
            // 🖱️ 右键按下：只负责平移 (且只有在没按左键时生效)
            else if (Input.GetMouseButton(1))
            {
                // 根据距离调整移动速度，离得越远动得越快
                float panFactor = manualDistance * 0.003f * editPanSensitivity;

                // delta.x 对应水平移动，delta.y 对应垂直移动
                targetPan.x += delta.x * panFactor;
                targetPan.y += delta.y * panFactor;
            }

            // 更新鼠标位置记录
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
                lastMousePos = Input.mousePosition;
        }

        // 滚轮缩放FOV
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && !isMouseOverUI)
        {
            fieldOfView -= scroll * 20f;
            fieldOfView = Mathf.Clamp(fieldOfView, 1f, 120f);
        }
    }

    void ApplyModelTransform()
    {
        // 旋转
        float baseX = fixRotation180 ? 180 : 0;
        Quaternion baseRot = Quaternion.Euler(baseX, 0, 0);
        Quaternion spinRot = Quaternion.Euler(targetRotX, targetRotY, 0);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRot * spinRot, Time.deltaTime * smoothSpeed);

        // 缩放 (带动画)
        float sX = mirrorX ? -modelScale : modelScale;
        float sY = flipY ? -modelScale : modelScale;
        Vector3 finalScale = new Vector3(sX, sY, modelScale) * currentIntroScale;

        if (introTimer < introDuration)
            transform.localScale = finalScale;
        else
            transform.localScale = Vector3.Lerp(transform.localScale, finalScale, Time.deltaTime * smoothSpeed);

        // 平移
        Vector3 finalPos = new Vector3(targetPan.x, targetPan.y, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPos, Time.deltaTime * smoothSpeed);
    }

    void ResetTransform()
    {
        targetRotY = 0f;
        targetRotX = 0f;
        targetPan = Vector2.zero;
        manualDistance = 2.5f;
        modelScale = 1.0f;
        fieldOfView = 10.0f;
        targetFOV = 10.0f;

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 0, -manualDistance);
            Camera.main.transform.LookAt(Vector3.zero);
        }
    }

    private void ReceiveData()
    {
        try
        {
            client = new UdpClient(udpPort);
            client.Client.ReceiveTimeout = 1000;
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            while (isRunning)
            {
                try
                {
                    byte[] data = client.Receive(ref anyIP);
                    string text = Encoding.UTF8.GetString(data);

                    if (isViewMode)
                    {
                        lastReceived = "✅ 生效中: " + text;
                        string[] parts = text.Split(',');
                        if (parts.Length >= 4)
                        {
                            float hX = float.Parse(parts[0]);
                            float hY = float.Parse(parts[1]);
                            float dist = float.Parse(parts[2]);

                            float pX = (hX - 0.5f) * panSensitivity;
                            float pY = (0.5f - hY) * panSensitivity;
                            targetPan = new Vector2(pX, pY);

                            float targetF = 60f - ((dist - 0.1f) * fovSensitivity * 10f);
                            targetFOV = Mathf.Clamp(targetF, 1f, 120f);
                        }
                    }
                    else
                    {
                        lastReceived = "⏸️ 编辑模式 (手势禁用)";
                    }
                }
                catch { }
            }
        }
        catch (System.Exception e) { lastReceived = "❌ " + e.Message; }
    }

    void OnGUI()
    {
        if (!showUI) return;
        GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
        windowRect = GUI.Window(0, windowRect, WindowFunction, "🛠️ 工作台 (Tab隐藏)");
    }

    void WindowFunction(int windowID)
    {
        GUI.backgroundColor = Color.white;
        GUI.contentColor = Color.white;

        GUILayout.BeginVertical();
        GUILayout.Space(5);

        string modeBtnText = isViewMode ? "👁️ 当前: 查看模式 (漫游)" : "🛠️ 当前: 编辑模式 (调整)";
        GUI.backgroundColor = isViewMode ? Color.green : Color.cyan;
        if (GUILayout.Button(modeBtnText, GUILayout.Height(35)))
        {
            isViewMode = !isViewMode;
            if (isViewMode)
            {
                RecordCameraRotation();
                targetFOV = fieldOfView;
            }
            else
            {
                targetPan = Vector2.zero; // 切回编辑模式归位模型
                fieldOfView = targetFOV;
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);
        if (isViewMode)
        {
            GUI.contentColor = Color.green;
            GUILayout.Label("💡 查看模式: WASD移动 | 右键旋转头", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }
        else
        {
            GUI.contentColor = Color.cyan;
            // ✅ 这里的提示现在是准确的
            GUILayout.Label("💡 编辑模式: 左键旋转 | 右键平移", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        }
        GUI.contentColor = Color.white;

        GUILayout.Space(10);

        if (!isViewMode)
        {
            GUILayout.Label("--- 🛠️ 参数调整 ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });

            GUILayout.Label($"📷 距离: {manualDistance:F2}m");
            manualDistance = GUILayout.HorizontalSlider(manualDistance, 0.5f, 8.0f);

            GUILayout.Label($"👁️ 视野 (FOV): {fieldOfView:F0}");
            fieldOfView = GUILayout.HorizontalSlider(fieldOfView, 1f, 120f);

            GUILayout.Label($"🔍 物理缩放: {modelScale:F2}");
            modelScale = GUILayout.HorizontalSlider(modelScale, 0.1f, 5.0f);

            GUILayout.Space(5);
            GUILayout.Label($"🖱️ 左键旋转灵敏度: {editMouseSensitivity:F2}");
            editMouseSensitivity = GUILayout.HorizontalSlider(editMouseSensitivity, 0.05f, 2.0f);

            GUILayout.Label($"🖱️ 右键平移灵敏度: {editPanSensitivity:F2}");
            editPanSensitivity = GUILayout.HorizontalSlider(editPanSensitivity, 0.1f, 5.0f);

            GUILayout.Space(5);
            if (GUILayout.Button("重置所有变换")) ResetTransform();

            GUILayout.Space(10);
            GUILayout.Label("--- 📐 朝向 ---");
            fixRotation180 = GUILayout.Toggle(fixRotation180, " 旋转 180°");
            mirrorX = GUILayout.Toggle(mirrorX, " 镜像翻转");
            flipY = GUILayout.Toggle(flipY, " Y轴翻转");
        }
        else
        {
            GUILayout.Label("--- 👁️ 漫游设置 ---", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });

            GUILayout.Label($"🚀 飞行速度: {flySpeed:F1}");
            flySpeed = GUILayout.HorizontalSlider(flySpeed, 0.1f, 10.0f);

            GUILayout.Label($"🖱️ 鼠标转向灵敏度: {mouseLookSensitivity:F1}");
            mouseLookSensitivity = GUILayout.HorizontalSlider(mouseLookSensitivity, 0.1f, 5.0f);

            GUILayout.Space(5);
            GUILayout.Label($"↔️ 手势平移灵敏度: {panSensitivity:F1}");
            panSensitivity = GUILayout.HorizontalSlider(panSensitivity, 0.1f, 5f);

            GUILayout.Label($"🔍 手势缩放灵敏度: {fovSensitivity:F0}");
            fovSensitivity = GUILayout.HorizontalSlider(fovSensitivity, 1f, 50f);

            GUILayout.Space(10);
            GUILayout.Label("👋 状态:");
            GUILayout.TextArea(lastReceived);
        }

        GUILayout.EndVertical();
        GUI.DragWindow();
    }

    void OnApplicationQuit() { isRunning = false; if (client != null) client.Close(); if (receiveThread != null) receiveThread.Abort(); }
}