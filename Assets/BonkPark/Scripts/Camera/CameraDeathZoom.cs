using System.Collections;
using UnityEngine;

// Pushes the camera in on Lumi when the run ends: glides toward the target and tightens the orthographic view.
[RequireComponent(typeof(Camera))]
public class CameraDeathZoom : MonoBehaviour
{
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void PlayZoom(Transform target, float duration, float targetSize)
    {
        // CameraShake snaps localPosition back to its base every LateUpdate; let it go so the push-in can travel.
        var shake = GetComponent<CameraShake>();
        if (shake != null) shake.enabled = false;
        StartCoroutine(ZoomRoutine(target, duration, targetSize));
    }

    IEnumerator ZoomRoutine(Transform target, float duration, float targetSize)
    {
        Vector3 from = transform.position;
        Vector3 to = target != null
            ? new Vector3(target.position.x, target.position.y, from.z)
            : from;
        float startSize = cam.orthographicSize;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            transform.position = Vector3.Lerp(from, to, k);
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, k);
            yield return null;
        }

        transform.position = to;
        cam.orthographicSize = targetSize;
    }
}
