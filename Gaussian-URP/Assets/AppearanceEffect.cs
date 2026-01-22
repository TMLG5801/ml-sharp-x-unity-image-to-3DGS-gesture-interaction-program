using UnityEngine;
using GaussianSplatting.Runtime;

public class AppearanceEffect : MonoBehaviour
{
    [Header("✨ 特效参数")]
    [Tooltip("特效持续时间 (秒)")]
    public float duration = 1.5f;

    [Tooltip("动画曲线 (更有弹性)")]
    public AnimationCurve growthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.05f), new Keyframe(1, 1));

    private float timer = 0f;
    private bool isAnimating = false;
    private Vector3 targetScale = Vector3.one;

    void Start()
    {
        // 1. 记录物体原本应该有的大小 (通常是 1,1,1)
        targetScale = transform.localScale;

        // 防止意外读取到 0
        if (targetScale == Vector3.zero) targetScale = Vector3.one;

        // 2. 🎬 动画开始前：先把物体缩到看不见 (0,0,0)
        transform.localScale = Vector3.zero;

        timer = 0f;
        isAnimating = true;
    }

    void LateUpdate()
    {
        if (!isAnimating) return;

        timer += Time.deltaTime;
        float progress = timer / duration;

        if (progress >= 1.0f)
        {
            // 3. 动画结束：恢复到目标大小
            transform.localScale = targetScale;
            isAnimating = false;
            // 任务完成，销毁这个脚本，节省性能
            Destroy(this);
        }
        else
        {
            // 4. 动画进行中：根据曲线计算当前大小
            float currentRatio = growthCurve.Evaluate(progress);
            transform.localScale = targetScale * currentRatio;
        }
    }
}