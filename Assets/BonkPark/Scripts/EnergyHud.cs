using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Corner readout of Lumi's energy: a number that warms toward gold as it fills and cools toward red as it
// drains, beside a fixed orb. When energy runs low both the number and the orb flicker to pull the eye,
// matching the panic in LumiEnergy's glow.
public class EnergyHud : MonoBehaviour
{
    [Tooltip("Energy source to mirror.")]
    [SerializeField] LumiEnergy lumi;

    [Tooltip("Current energy, shown as a whole number.")]
    [SerializeField] TMP_Text valueLabel;

    [Tooltip("Orb icon tinted by the current charge.")]
    [SerializeField] Image orb;

    [Header("Colour")]

    [Tooltip("Tint at full charge.")]
    [SerializeField] Color highColor = new Color(1f, 0.79f, 0.32f);

    [Tooltip("Tint at empty.")]
    [SerializeField] Color lowColor = new Color(0.78f, 0.16f, 0.12f);

    [Header("Low warning")]

    [Tooltip("Flicker speed when energy is low.")]
    [SerializeField] float lowFlickerSpeed = 12f;

    [Tooltip("Flicker depth when energy is low, fraction of full alpha.")]
    [SerializeField] float lowFlickerAmount = 0.35f;

    void Update()
    {
        if (lumi == null) return;

        float n = lumi.Normalized;
        Color c = Color.Lerp(lowColor, highColor, n);
        float alpha = lumi.IsLowLight ? LowFlicker() : 1f;

        if (valueLabel != null)
        {
            valueLabel.text = Mathf.CeilToInt(lumi.Energy).ToString();
            valueLabel.color = new Color(c.r, c.g, c.b, alpha);
        }
        // The orb keeps its own art; only its alpha flickers when energy is low.
        if (orb != null) orb.color = new Color(1f, 1f, 1f, alpha);
    }

    float LowFlicker()
    {
        float noise = Mathf.PerlinNoise(Time.time * lowFlickerSpeed, 0f);
        return 1f - lowFlickerAmount * noise;
    }
}
