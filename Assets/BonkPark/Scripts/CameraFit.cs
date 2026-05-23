using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [SerializeField] Park park;

    void Start()
    {
        GetComponent<Camera>().orthographicSize = park.ReferenceViewport.y * 0.5f;
    }
}
