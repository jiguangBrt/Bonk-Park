using UnityEngine;

// Keeps the camera at Park.TargetAspect by shrinking Camera.rect, producing pillarbox or letterbox bars as needed.
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class LetterboxCamera : MonoBehaviour
{
    [Tooltip("Park reference.")]
    [SerializeField] Park park;

    Camera cam;
    int lastW, lastH;
    float lastAspect;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void Update()
    {
        if (park == null) return;
        if (!ViewportChanged()) return;
        Apply();
    }

    bool ViewportChanged()
    {
        return Screen.width != lastW || Screen.height != lastH || park.TargetAspect != lastAspect;
    }

    void Apply()
    {
        if (park == null) return;
        // Editor can repaint with a zero-sized screen (Game View hidden/collapsed); skip until it has real dimensions.
        if (Screen.width <= 0 || Screen.height <= 0) return;
        lastW = Screen.width;
        lastH = Screen.height;
        lastAspect = park.TargetAspect;

        float windowAspect = (float)Screen.width / Screen.height;
        float scale = windowAspect / park.TargetAspect;

        // scale < 1: window narrower than target -> letterbox; scale >= 1: wider -> pillarbox.
        if (scale < 1f)
            cam.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        else
        {
            float inv = 1f / scale;
            cam.rect = new Rect((1f - inv) * 0.5f, 0f, inv, 1f);
        }
    }
}
