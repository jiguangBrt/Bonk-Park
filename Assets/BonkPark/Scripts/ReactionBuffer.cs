using UnityEngine;

// Ring buffer of recent positions. Sampled every FixedUpdate so enemies can query Lumi's position from N seconds ago.
public class ReactionBuffer : MonoBehaviour
{
    [Tooltip("Max queryable delay, seconds.")]
    [SerializeField] float maxDelay = 1.0f;

    Vector2[] positions;
    float[] times;
    int head;
    int count;

    void Awake()
    {
        int capacity = Mathf.CeilToInt(maxDelay / Time.fixedDeltaTime) + 8;
        positions = new Vector2[capacity];
        times = new float[capacity];
    }

    void FixedUpdate()
    {
        positions[head] = (Vector2)transform.position;
        times[head] = Time.fixedTime;
        head = (head + 1) % positions.Length;
        if (count < positions.Length) count++;
    }

    // Returns the most recent sample at or before delaySeconds ago, or the oldest available if history is too short.
    public Vector2 GetSnapshot(float delaySeconds)
    {
        if (count == 0) return transform.position;
        float target = Time.fixedTime - delaySeconds;

        // Scan newest to oldest for the first sample with time <= target.
        for (int i = 1; i <= count; i++)
        {
            int idx = (head - i + positions.Length) % positions.Length;
            if (times[idx] <= target) return positions[idx];
        }

        // Fallback: oldest sample. Index 0 before the buffer fills, head once it has wrapped.
        int oldest = count < positions.Length ? 0 : head;
        return positions[oldest];
    }
}
