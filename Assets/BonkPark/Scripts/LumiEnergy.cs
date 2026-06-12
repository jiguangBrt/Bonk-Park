using UnityEngine;
using UnityEngine.Rendering.Universal;

// Lumi's glow reads its energy: the lights warm at full charge, fade to dark red as it drains, and pulse gently like a real firefly. The sprite itself stays untinted.
// Runs in edit mode too, so the glow lights mirror highColor live in the Scene view for colour tuning.
[ExecuteAlways]
public class LumiEnergy : MonoBehaviour
{
    [Header("Energy")]

    [Tooltip("Energy ceiling.")]
    [SerializeField] float maxEnergy = 100f;

    [Tooltip("Passive energy drain, per second.")]
    [SerializeField] float drainRate = 5f;

    [Tooltip("Below this fraction of max, Lumi slows and the glow flickers red.")]
    [Range(0f, 1f)]
    [SerializeField] float lowLightThreshold = 0.3f;

    [Tooltip("Current energy. Drag at runtime to preview the glow.")]
    [SerializeField] float energy = 100f;

    [Header("Glow")]

    [Tooltip("Lights driven by energy. Their colour follows highColor.")]
    [SerializeField] Light2D[] glowLights;

    [Tooltip("Glow colour at full energy. Edit here to tune both lights.")]
    [SerializeField] Color highColor = new Color(1f, 0.729f, 0.608f);

    [Tooltip("Glow colour at empty energy.")]
    [SerializeField] Color lowColor = new Color(0.494f, 0.004f, 0f);

    [Tooltip("Glow pulse speed.")]
    [SerializeField] float breathSpeed = 3f;

    [Tooltip("Pulse depth, fraction of base intensity.")]
    [SerializeField] float breathAmount = 0.3f;

    [Tooltip("Extra glow shrink at empty light, fraction of base intensity.")]
    [SerializeField] float lowLightDim = 0.4f;

    [Tooltip("Flicker speed when light is low.")]
    [SerializeField] float lowLightFlickerSpeed = 14f;

    [Tooltip("Flicker depth when light is low, fraction of base intensity.")]
    [SerializeField] float lowLightFlickerAmount = 0.35f;

    [Tooltip("Dash glow pop, multiple of base intensity.")]
    [SerializeField] float dashFlarePeak = 1.8f;

    [Tooltip("Dash glow pop fade-out, s.")]
    [SerializeField] float dashFlareDuration = 0.35f;

    float[] baseIntensities;
    float igniteDuration;
    float igniteElapsed;
    bool igniting;
    float flareElapsed;
    bool flaring;
    float extinguishDuration;
    float extinguishElapsed;
    bool extinguishing;

    public float Normalized => energy / maxEnergy;
    public float Energy => energy;
    public float MaxEnergy => maxEnergy;
    public float LowLightThreshold => lowLightThreshold;
    public bool IsLowLight => Normalized < lowLightThreshold;

    void Awake()
    {
        if (!Application.isPlaying) return;
        energy = maxEnergy;
        if (glowLights == null) return;
        baseIntensities = new float[glowLights.Length];
        for (int i = 0; i < glowLights.Length; i++)
        {
            if (glowLights[i] != null) baseIntensities[i] = glowLights[i].intensity;
        }
    }

    // Snap the lights to highColor as soon as the component loads or recompiles in the editor.
    void OnEnable()
    {
        if (!Application.isPlaying) SyncEditorColor();
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            SyncEditorColor();
            return;
        }
        energy = Mathf.Max(0f, energy - drainRate * Time.deltaTime);
        if (igniting)
        {
            igniteElapsed += Time.deltaTime;
            if (igniteElapsed >= igniteDuration) igniting = false;
        }
        if (flaring) flareElapsed += Time.deltaTime;
        if (extinguishing)
            extinguishElapsed = Mathf.Min(extinguishDuration, extinguishElapsed + Time.deltaTime);
        ApplyGlow();
    }

    // Drain hook for future systems (brake, hazards).
    public void Consume(float amount)
    {
        energy = Mathf.Max(0f, energy - amount);
    }

    // Reward hook for future systems (bonk); flares the glow back up.
    public void Add(float amount)
    {
        energy = Mathf.Min(maxEnergy, energy + amount);
    }

    // Ignition ramp for the intro handoff; the glow swells from dark to full over the given duration.
    public void Ignite(float duration)
    {
        igniteDuration = Mathf.Max(0.0001f, duration);
        igniteElapsed = 0f;
        igniting = true;
    }

    // Dash hook: pops the glow to dashFlarePeak, then lets it contract back to base over dashFlareDuration.
    public void Flare()
    {
        flareElapsed = 0f;
        flaring = true;
    }

    // Death hook: fades the glow out over the given duration and holds it dark.
    public void Extinguish(float duration)
    {
        extinguishDuration = Mathf.Max(0.0001f, duration);
        extinguishElapsed = 0f;
        extinguishing = true;
    }

    void ApplyGlow()
    {
        if (glowLights == null) return;
        Color c = Color.Lerp(lowColor, highColor, Normalized);
        float ignite = igniting ? Mathf.SmoothStep(0f, 1f, igniteElapsed / igniteDuration) : 1f;
        float extinguish = extinguishing ? Mathf.SmoothStep(1f, 0f, extinguishElapsed / extinguishDuration) : 1f;
        float pulse = GlowPulse();
        float flare = DashFlare();
        for (int i = 0; i < glowLights.Length; i++)
        {
            var glow = glowLights[i];
            if (glow == null) continue;
            glow.color = c;
            glow.intensity = baseIntensities[i] * pulse * ignite * flare * extinguish;
        }
    }

    // Dash flare: snaps to dashFlarePeak on the press, then eases back to 1 over dashFlareDuration.
    float DashFlare()
    {
        if (!flaring) return 1f;
        float t = flareElapsed / dashFlareDuration;
        if (t >= 1f) { flaring = false; return 1f; }
        return Mathf.Lerp(dashFlarePeak, 1f, t);
    }

    // Gentle sine breath at full charge; below the threshold it crossfades into a faster, noisier
    // flicker and the whole glow dims, so a starving firefly reads as panic.
    float GlowPulse()
    {
        float breath = 1f + breathAmount * Mathf.Sin(Time.time * breathSpeed);
        if (Normalized >= lowLightThreshold) return breath;

        float starve = 1f - Normalized / lowLightThreshold;
        float noise = Mathf.PerlinNoise(Time.time * lowLightFlickerSpeed, 0f) * 2f - 1f;
        float flicker = 1f + lowLightFlickerAmount * noise;
        return Mathf.Lerp(breath, flicker, starve) * (1f - lowLightDim * starve);
    }

    // Edit-mode preview: keep both lights matching highColor (colour only, intensity untouched) so the glow can be tuned in the Scene view.
    void SyncEditorColor()
    {
        if (glowLights == null) return;
        foreach (var glow in glowLights)
        {
            if (glow != null && !glow.color.Equals(highColor)) glow.color = highColor;
        }
    }
}
