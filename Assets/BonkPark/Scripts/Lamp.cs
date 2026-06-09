using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// A bonkable stone lantern. Only the base block is solid; a bat bonk short-circuits the lit
// windows so the glow stutters out a few times before settling back to full.
public class Lamp : Bonkable
{
    [Header("Glow")]

    [Tooltip("The lit-window light that stutters on a bonk.")]
    [SerializeField] Light2D glow;

    [Tooltip("The lantern sprite, dimmed slightly while the glow is out.")]
    [SerializeField] SpriteRenderer body;

    [Header("Short Circuit")]

    [Tooltip("How long the stutter lasts before recovery, s.")]
    [SerializeField] float flickerDuration = 0.9f;

    [Tooltip("Min/max gap between flicker flips, s.")]
    [SerializeField] Vector2 flickerInterval = new Vector2(0.04f, 0.13f);

    [Tooltip("Glow level on an off-flash, fraction of base. 0 = fully out.")]
    [Range(0f, 1f)]
    [SerializeField] float dimLevel = 0.05f;

    [Tooltip("Ramp back to full glow, s.")]
    [SerializeField] float recovery = 0.35f;

    [Tooltip("How dark the lantern sprite goes at full-off. Keep subtle.")]
    [Range(0f, 1f)]
    [SerializeField] float bodyDim = 0.25f;

    float baseIntensity;
    Color baseColor;
    Color darkColor;
    Coroutine routine;

    void Awake()
    {
        if (glow != null) baseIntensity = glow.intensity;
        if (body != null)
        {
            baseColor = body.color;
            darkColor = Color.Lerp(baseColor, Color.black, bodyDim);
        }
    }

    public override void OnBonk()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShortCircuit());
    }

    IEnumerator ShortCircuit()
    {
        float elapsed = 0f;
        bool on = false;
        while (elapsed < flickerDuration)
        {
            on = !on;
            SetGlow(on ? 1f : dimLevel);
            float step = Random.Range(flickerInterval.x, flickerInterval.y);
            elapsed += step;
            yield return new WaitForSeconds(step);
        }

        float t = 0f;
        while (t < recovery)
        {
            t += Time.deltaTime;
            SetGlow(Mathf.Lerp(dimLevel, 1f, t / recovery));
            yield return null;
        }
        SetGlow(1f);
        routine = null;
    }

    void SetGlow(float level)
    {
        if (glow != null) glow.intensity = baseIntensity * level;
        if (body != null) body.color = Color.Lerp(darkColor, baseColor, level);
    }
}
