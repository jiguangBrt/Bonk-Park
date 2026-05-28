using UnityEngine;

// Offsets camera localPosition by a random vector each frame for a brief impact shake; magnitude decays linearly to zero.
public class CameraShake : MonoBehaviour
{
    Vector3 basePos;
    float remaining;
    float duration;
    float magnitude;

    void Awake()
    {
        basePos = transform.localPosition;
    }

    public void Shake(float dur, float mag)
    {
        if (dur <= 0f || mag <= 0f) return;
        duration = dur;
        remaining = dur;
        magnitude = mag;
    }

    void LateUpdate()
    {
        if (remaining <= 0f)
        {
            RestoreBasePosition();
            return;
        }
        ApplyShakeOffset();
    }

    void RestoreBasePosition()
    {
        if (transform.localPosition != basePos) transform.localPosition = basePos;
    }

    void ApplyShakeOffset()
    {
        remaining -= Time.deltaTime;
        float t = Mathf.Clamp01(remaining / duration);
        Vector2 offset = Random.insideUnitCircle * (magnitude * t);
        transform.localPosition = basePos + (Vector3)offset;
    }
}
