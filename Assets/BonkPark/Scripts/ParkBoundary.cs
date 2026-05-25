using UnityEngine;

// Rebuilds the EdgeCollider2D as a closed rectangle matching Park.Size; runs on Awake and on inspector changes.
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
        // Closed rectangle: BL -> BR -> TR -> TL -> BL.
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
