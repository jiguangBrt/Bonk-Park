using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [SerializeField] Park park;
    [SerializeField, Range(0f, 0.5f)] float padding = 0.05f;

    void Start()
    {
        var cam = GetComponent<Camera>();
        var halfH = park.Size.y * 0.5f;
        var halfW = park.Size.x * 0.5f;
        cam.orthographicSize = Mathf.Max(halfH, halfW / cam.aspect) * (1f + padding);
    }
}
