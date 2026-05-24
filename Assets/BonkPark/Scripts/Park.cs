using UnityEngine;

// 公园场景的尺寸定义中心。referenceViewport 是设计画面（相机看到的范围），
// arenaSize 是实际可活动区域（比相机视野稍大，留出边距防止 Lumi 贴边时看到外部）。
// CameraFit 据此设相机正交大小，ParkBoundary 据此摆放围墙。
public class Park : MonoBehaviour
{
    [Tooltip("设计目标的相机视野大小（世界单位，宽 × 高）。Full HD 16:9 对应 19.2 × 10.8，相机的 orthographicSize = 这里的 y / 2。")]
    [SerializeField] Vector2 referenceViewport = new Vector2(19.2f, 10.8f);

    [Tooltip("实际可活动场地尺寸（世界单位）。比 referenceViewport 大一圈，留出 buffer 防止边界 collider 暴露在相机视野里。")]
    [SerializeField] Vector2 arenaSize = new Vector2(21f, 11.8f);

    public Vector2 ReferenceViewport => referenceViewport;
    public Vector2 Size => arenaSize;
    public float TargetAspect => referenceViewport.x / referenceViewport.y;

    // 编辑器里画两个线框：青色 = 实际场地范围，黄色 = 相机参考视野
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(arenaSize.x, arenaSize.y, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(referenceViewport.x, referenceViewport.y, 0f));
    }
}
