using UnityEngine;

// 锁定相机比例 = Park.TargetAspect，避免不同分辨率拉伸画面。窗口比目标更宽时左右
// 加黑边（pillarbox），更窄时上下加黑边（letterbox）。通过调 Camera.rect 实现：
// rect 是 viewport 在屏幕里的归一化区域，缩窄它就在剩余位置自然形成黑边。
// ExecuteAlways 让编辑器里调窗口尺寸时实时刷新，所见即所得。
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class LetterboxCamera : MonoBehaviour
{
    [Tooltip("提供 TargetAspect 的 Park 组件。一般直接拖场景里的 Park 物体。")]
    [SerializeField] Park park;

    Camera cam;
    int lastW, lastH;       // 记录上次窗口尺寸，避免每帧重算 cam.rect
    float lastAspect;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void Update()
    {
        if (park == null) return;
        // 窗口和目标比例都没变就跳过，避免无意义的 Camera.rect 重设
        if (Screen.width == lastW && Screen.height == lastH && park.TargetAspect == lastAspect) return;
        Apply();
    }

    void Apply()
    {
        if (park == null) return;
        lastW = Screen.width;
        lastH = Screen.height;
        lastAspect = park.TargetAspect;

        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / park.TargetAspect;

        // scale < 1：窗口比目标更窄（更"竖"），上下加黑边，rect 高度缩到 scale
        // scale >= 1：窗口比目标更宽，左右加黑边，rect 宽度缩到 1/scale
        if (scale < 1f)
            cam.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        else
        {
            float inv = 1f / scale;
            cam.rect = new Rect((1f - inv) * 0.5f, 0f, inv, 1f);
        }
    }
}
