using UnityEngine;
using UnityEngine.UI;

// A scatter of firefly motes that drift and twinkle around the title, like Lumi's kin hovering over the wordmark.
// Each child graphic wanders on slow Perlin noise around where it started and breathes its glow, on unscaled time.
public class FloatingMotes : MonoBehaviour
{
    [Tooltip("How far a mote wanders from its start point.")]
    [SerializeField] float drift = 55f;
    [SerializeField] float driftSpeed = 0.12f;
    [SerializeField] float twinkleSpeed = 0.9f;

    Graphic[] gfx;
    RectTransform[] rts;
    Vector2[] home;
    float[] baseAlpha;

    void Awake()
    {
        int n = transform.childCount;
        gfx = new Graphic[n]; rts = new RectTransform[n]; home = new Vector2[n]; baseAlpha = new float[n];
        for (int i = 0; i < n; i++)
        {
            var c = transform.GetChild(i);
            rts[i] = c as RectTransform;
            gfx[i] = c.GetComponent<Graphic>();
            home[i] = rts[i] != null ? rts[i].anchoredPosition : Vector2.zero;
            baseAlpha[i] = gfx[i] != null ? gfx[i].color.a : 1f;
        }
    }

    void Update()
    {
        float t = Time.unscaledTime;
        for (int i = 0; i < rts.Length; i++)
        {
            if (rts[i] == null) continue;
            float s = i * 1.37f;
            float dx = (Mathf.PerlinNoise(s, t * driftSpeed) - 0.5f) * 2f * drift;
            float dy = (Mathf.PerlinNoise(s + 9.2f, t * driftSpeed) - 0.5f) * 2f * drift;
            rts[i].anchoredPosition = home[i] + new Vector2(dx, dy);

            if (gfx[i] != null)
            {
                float tw = 0.55f + 0.45f * Mathf.Sin(t * twinkleSpeed + s * 2.3f);
                var col = gfx[i].color; col.a = baseAlpha[i] * tw; gfx[i].color = col;
            }
            float sc = 0.85f + 0.15f * Mathf.Sin(t * twinkleSpeed * 0.6f + s);
            rts[i].localScale = new Vector3(sc, sc, 1f);
        }
    }
}
