using UnityEngine;

// Locks the orthographic size to Park.referenceViewport so world-unit framing stays consistent across resolutions.
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [Tooltip("Park reference.")]
    [SerializeField] Park park;

    void Start()
    {
        GetComponent<Camera>().orthographicSize = park.ReferenceViewport.y * 0.5f;
    }
}
