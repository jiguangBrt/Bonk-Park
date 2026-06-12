using UnityEngine;

// Tints Lumi's motion trail to match her glow: it warms and dims with energy, brightens on the dash, and
// stops emitting when the controller is cut on death so no streak hangs in the air. Length tracks speed on
// its own — the TrailRenderer drops vertices by distance, so a fast Lumi leaves a longer tail.
[RequireComponent(typeof(TrailRenderer))]
public class LumiTrail : MonoBehaviour
{
    [Tooltip("Source of charge and the dash state.")]
    [SerializeField] LumiEnergy energy;

    [SerializeField] PlayerController controller;

    [Tooltip("Trail colour at full energy. Mirrors LumiEnergy's highColor.")]
    [SerializeField] Color highColor = new Color(1f, 0.729f, 0.608f);

    [Tooltip("Trail colour at empty energy.")]
    [SerializeField] Color lowColor = new Color(0.494f, 0.004f, 0f);

    [Tooltip("Head opacity at full energy.")]
    [Range(0f, 1f)]
    [SerializeField] float baseAlpha = 0.8f;

    [Tooltip("Opacity multiplier while dashing, for a hotter streak.")]
    [SerializeField] float dashBoost = 1.4f;

    TrailRenderer trail;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        if (energy == null) energy = GetComponentInParent<LumiEnergy>();
        if (controller == null) controller = GetComponentInParent<PlayerController>();
    }

    void LateUpdate()
    {
        float charge = energy != null ? energy.Normalized : 1f;
        Color c = Color.Lerp(lowColor, highColor, charge);

        float a = baseAlpha;
        if (controller != null && controller.Dashing) a = Mathf.Min(1f, a * dashBoost);

        trail.startColor = new Color(c.r, c.g, c.b, a);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);

        trail.emitting = controller == null || controller.enabled;
    }
}
