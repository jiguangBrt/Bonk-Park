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

    float[] baseIntensities;
    float igniteDuration;
    float igniteElapsed;
    bool igniting;

    public float Normalized => energy / maxEnergy;

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

    void ApplyGlow()
    {
        if (glowLights == null) return;
        Color c = Color.Lerp(lowColor, highColor, Normalized);
        float breath = 1f + breathAmount * Mathf.Sin(Time.time * breathSpeed);
        float ignite = igniting ? Mathf.SmoothStep(0f, 1f, igniteElapsed / igniteDuration) : 1f;
        for (int i = 0; i < glowLights.Length; i++)
        {
            var glow = glowLights[i];
            if (glow == null) continue;
            glow.color = c;
            glow.intensity = baseIntensities[i] * breath * ignite;
        }
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
