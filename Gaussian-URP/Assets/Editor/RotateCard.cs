using UnityEngine;

public class RotateCard : MonoBehaviour
{
    // 旋转速度
    public float sensitivity = 0.5f;

    // 鼠标上一帧的位置
    private Vector3 lastMousePosition;

    void Update()
    {
        // 当按下鼠标左键时
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }

        // 当按住鼠标左键拖动时
        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;

            // === 核心修正 ===
            // 我们使用 Space.World (世界坐标) 来旋转，这样就不受模型 Scale -1 的影响了
            // 也不受模型当前角度的影响，始终：
            // 鼠标往左滑 -> 模型往左转 (绕世界 Y 轴)
            // 鼠标往上滑 -> 模型往下转 (绕世界 X 轴)

            // 注意：这里 delta.x 前面加负号还是正号，取决于你想要“拨动地球仪”还是“控制摄像机”的感觉
            // 当前设置：鼠标往左(-x)，模型绕Y轴正向转，看着像物体在跟手转
            transform.Rotate(Vector3.up, -delta.x * sensitivity, Space.World);
            transform.Rotate(Vector3.right, delta.y * sensitivity, Space.World);

            lastMousePosition = Input.mousePosition;
        }

        // 滚轮缩放 (可选)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            transform.localScale += Vector3.one * scroll * (transform.localScale.x > 0 ? 1 : -1);
        }
    }
}