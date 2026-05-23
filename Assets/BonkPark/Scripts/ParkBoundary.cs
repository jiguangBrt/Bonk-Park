using UnityEngine;

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
