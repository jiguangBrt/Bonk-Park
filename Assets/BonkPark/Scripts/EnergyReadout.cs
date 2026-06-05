using UnityEngine;

// Playtest aid: tick `show` in the inspector to print Lumi's current light on screen.
// Off by default so it never appears in a normal run.
[RequireComponent(typeof(LumiEnergy))]
public class EnergyReadout : MonoBehaviour
{
    [Tooltip("Show the current energy value on screen.")]
    [SerializeField] bool show;

    LumiEnergy energy;
    GUIStyle style;

    void Awake()
    {
        energy = GetComponent<LumiEnergy>();
    }

    void OnGUI()
    {
        if (!show) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
        }
        style.normal.textColor = Color.Lerp(Color.red, Color.yellow, energy.Normalized);
        string text = "Light  " + Mathf.RoundToInt(energy.Energy) + " / " + Mathf.RoundToInt(energy.MaxEnergy)
            + "   (" + Mathf.RoundToInt(energy.Normalized * 100f) + "%)";
        GUI.Label(new Rect(14, 10, 400, 36), text, style);
    }
}
