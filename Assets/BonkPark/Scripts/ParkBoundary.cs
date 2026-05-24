using UnityEngine;

// 根据 Park.Size 自动重建 EdgeCollider2D 的四个角点，给场地围一圈不可穿越的边。
// 同挂在 Park 上，OnValidate 让编辑器里改 Park 尺寸时实时同步、Awake 保证运行时初次构建。
// 没有任何 SerializeField：所有数据来自同物体的 Park 组件。
[RequireComponent(typeof(EdgeCollider2D), typeof(Park))]
public class ParkBoundary : MonoBehaviour
{
    void OnValidate() => Rebuild();
    void Awake() => Rebuild();

    void Rebuild()
    {
        var park = GetComponent<Park>();
        var edge = GetComponent<EdgeCollider2D>();
        var hw = park.Size.x * 0.5f;
        var hh = park.Size.y * 0.5f;
        // 闭合矩形：左下 → 右下 → 右上 → 左上 → 回到左下
        edge.points = new[]
        {
            new Vector2(-hw, -hh),
            new Vector2( hw, -hh),
            new Vector2( hw,  hh),
            new Vector2(-hw,  hh),
            new Vector2(-hw, -hh),
        };
    }
}
