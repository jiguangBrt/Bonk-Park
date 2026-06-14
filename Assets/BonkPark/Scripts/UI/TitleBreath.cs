using TMPro;
using UnityEngine;

// The title breathes like a firefly: its face glow swells and settles so the bloom around the wordmark drifts in and
// out instead of sitting flat. Runs on unscaled time so it keeps breathing even if the game is paused.
public class TitleBreath : MonoBehaviour
{
    [SerializeField] TMP_Text title;

    [Tooltip("Dimmest and brightest the glow reaches, as a multiple of the title's set face colour.")]
    [SerializeField] float low = 0.8f;
    [SerializeField] float high = 1.3f;

    [Tooltip("Breath speed; lower is slower.")]
    [SerializeField] float speed = 1.1f;

    static readonly int FaceColor = Shader.PropertyToID("_FaceColor");
    Material mat;
    Color baseFace;

    void Awake()
    {
        if (title == null) title = GetComponent<TMP_Text>();
        mat = title.fontMaterial;
        baseFace = mat.GetColor(FaceColor);
    }

    void Update()
    {
        float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed);
        Color c = baseFace * Mathf.Lerp(low, high, k);
        c.a = baseFace.a;
        mat.SetColor(FaceColor, c);
    }
}
