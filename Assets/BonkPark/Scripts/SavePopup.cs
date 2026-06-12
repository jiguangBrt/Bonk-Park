using TMPro;
using UnityEngine;

// A short world-space note that drifts up and fades out, then removes itself. Spawned each time a firefly reaches the meadow.
public class SavePopup : MonoBehaviour
{
    [Tooltip("How far the note drifts up, world units.")]
    [SerializeField] float rise = 0.8f;

    [Tooltip("Lifetime before it fades out and is removed, seconds.")]
    [SerializeField] float lifetime = 1.5f;

    [Tooltip("Share of the lifetime spent fading in.")]
    [Range(0f, 1f)]
    [SerializeField] float fadeInShare = 0.25f;

    TMP_Text label;
    Vector3 from;
    float elapsed;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        from = transform.position;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        transform.position = from + Vector3.up * (rise * Mathf.SmoothStep(0f, 1f, t));

        float alpha = t < fadeInShare
            ? t / fadeInShare
            : 1f - (t - fadeInShare) / (1f - fadeInShare);
        label.alpha = Mathf.Clamp01(alpha);

        if (elapsed >= lifetime) Destroy(gameObject);
    }
}
