using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// First-run opening storyboard on a black screen: each picture warms up from its centre outward, its two narration lines
// glow in and out one at a time, then it dims back to black before the next. The final picture hands control to the live
// game as Lumi's light ignites. Any key skips straight to the handoff.
public class IntroSequence : MonoBehaviour
{
    [Header("Content")]

    [Tooltip("Story pictures, shown in order.")]
    [SerializeField] Sprite[] panels;

    [Tooltip("Narration lines, read in order and dealt out across the pictures by linesPerPanel.")]
    [TextArea] [SerializeField] string[] lines;

    [Tooltip("How many narration lines each picture carries, in order; should sum to lines.Length.")]
    [SerializeField] int[] linesPerPanel = { 2, 2, 2, 2 };

    [Header("Timing, seconds")]

    [SerializeField] float revealIn = 1.4f;
    [SerializeField] float revealOut = 1.1f;
    [SerializeField] float lineHold = 2.4f;

    [Tooltip("Longer hold for the closing line as control hands over.")]
    [SerializeField] float finalLineHold = 4f;
    [SerializeField] float textFadeIn = 1.1f;
    [SerializeField] float textFadeOut = 0.6f;
    [SerializeField] float batBeat = 1.8f;

    [Tooltip("How long the scene lights swell up as control hands over.")]
    [SerializeField] float igniteDuration = 1.8f;

    [Header("Firefly breath")]

    [Tooltip("Narration pulse speed; match LumiEnergy.")]
    [SerializeField] float breathSpeed = 3f;

    [Tooltip("Narration pulse depth.")]
    [SerializeField] float breathAmount = 0.2f;

    [Header("Reveal")]

    [Tooltip("Per-pixel ramp width of the warm-up; larger = softer, slower swell.")]
    [SerializeField] float softness = 0.45f;

    [Header("Scene refs")]

    [SerializeField] PlayerController player;
    [SerializeField] MonoBehaviour batAI;
    [SerializeField] LumiEnergy lumi;
    [SerializeField] Image panelImage;
    [SerializeField] CanvasGroup rootGroup;
    [SerializeField] CanvasGroup batEyes;
    [SerializeField] TMP_Text narration;

#if UNITY_EDITOR
    [Header("Editor")]

    [Tooltip("Replay every play, ignoring the saved flag.")]
    [SerializeField] bool forceReplayInEditor;
#endif

    const string SeenKey = "IntroPlayed";

    Material mat;
    float currentProgress;
    bool skipRequested;
    bool playing;

    void Awake()
    {
        bool seen = PlayerPrefs.GetInt(SeenKey, 0) == 1;
#if UNITY_EDITOR
        if (forceReplayInEditor) seen = false;
#endif
        if (seen)
        {
            gameObject.SetActive(false);
            return;
        }

        mat = Instantiate(panelImage.material);
        panelImage.material = mat;
        mat.SetFloat("_Softness", softness);
        mat.SetFloat("_Aspect", 16f / 9f);
        mat.SetVector("_Center", new Vector4(0.5f, 0.5f, 0, 0));
        SetProgress(0f);

        if (rootGroup != null) rootGroup.alpha = 1f;
        narration.alpha = 0f;
        if (batEyes != null) batEyes.alpha = 0f;

        if (player != null) player.enabled = false;
        if (batAI != null) batAI.enabled = false;
        if (lumi != null) lumi.enabled = false;

        playing = true;
        StartCoroutine(PlayRoutine());
    }

    void Update()
    {
        if (playing && !skipRequested && Input.anyKeyDown) skipRequested = true;
    }

    void SetProgress(float p)
    {
        currentProgress = p;
        mat.SetFloat("_Progress", p);
    }

    IEnumerator PlayRoutine()
    {
        int line = 0;
        for (int i = 0; i < panels.Length; i++)
        {
            int count = i < linesPerPanel.Length ? linesPerPanel[i] : 0;
            yield return PanelRoutine(i, line, count);
            line += count;
            if (skipRequested) break;
            if (i == 2) { yield return BatEyesRoutine(); if (skipRequested) break; }
        }
        yield return HandoffRoutine();
    }

    IEnumerator PanelRoutine(int i, int firstLine, int count)
    {
        panelImage.sprite = panels[i];
        panelImage.color = Color.white;
        narration.alpha = 0f;

        yield return RevealRoutine(0f, 1f, revealIn);
        if (skipRequested) yield break;

        for (int n = 0; n < count; n++)
        {
            int index = firstLine + n;
            float hold = index == lines.Length - 1 ? finalLineHold : lineHold;
            yield return LineRoutine(index, hold);
            if (skipRequested) yield break;
        }

        yield return RevealRoutine(currentProgress, 0f, revealOut);
    }

    // One narration line glows in (then breathes), holds, and fades back out.
    IEnumerator LineRoutine(int index, float hold)
    {
        narration.text = index < lines.Length ? lines[index] : "";
        narration.alpha = 0f;

        float t = 0f;
        float total = textFadeIn + hold;
        while (t < total)
        {
            t += Time.deltaTime;
            float fade = textFadeIn > 0f ? Mathf.Clamp01(t / textFadeIn) : 1f;
            float breath = 0.8f + breathAmount * Mathf.Sin(Time.time * breathSpeed);
            narration.alpha = fade * breath;
            if (skipRequested) yield break;
            yield return null;
        }

        yield return FadeNarration(narration.alpha, 0f, textFadeOut);
    }

    // Two faint red eyes surface in the black before the bat picture.
    IEnumerator BatEyesRoutine()
    {
        narration.alpha = 0f;
        yield return Fade(batEyes, 0f, 0.55f, batBeat * 0.4f);
        float linger = batBeat * 0.3f;
        float t = 0f;
        while (t < linger)
        {
            t += Time.deltaTime;
            if (skipRequested) yield break;
            yield return null;
        }
        yield return Fade(batEyes, batEyes != null ? batEyes.alpha : 0f, 0f, batBeat * 0.3f);
    }

    // Dim the last picture, then let the whole scene warm up from black — the global light and Lumi swelling
    // together — before the bat is set loose. The player has control the moment the lights start to rise.
    IEnumerator HandoffRoutine()
    {
        if (currentProgress > 0.001f)
            yield return RevealRoutine(currentProgress, 0f, revealOut * 0.6f);

        narration.alpha = 0f;
        if (batEyes != null) batEyes.alpha = 0f;

        // Drop the picture so the reveal is a clean full-screen dissolve, not a lingering dark rectangle.
        if (panelImage != null) panelImage.enabled = false;

        // Find the scene's global light and take everything that should warm up down to black.
        Light2D global = null;
        foreach (var l in FindObjectsOfType<Light2D>())
            if (l.lightType == Light2D.LightType.Global) { global = l; break; }
        float globalBase = global != null ? global.intensity : 0f;
        if (global != null) global.intensity = 0f;

        if (lumi != null) { lumi.enabled = true; lumi.Ignite(igniteDuration); }
        if (player != null) player.enabled = true;

        float t = 0f;
        while (t < igniteDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / igniteDuration);
            if (rootGroup != null) rootGroup.alpha = 1f - Mathf.Clamp01(k * 2f); // overlay clears over the first half
            if (global != null) global.intensity = globalBase * k;
            yield return null;
        }

        if (rootGroup != null) rootGroup.alpha = 0f;
        if (global != null) global.intensity = globalBase;

        // Scene is fully lit — now the chase begins.
        if (batAI != null) batAI.enabled = true;

        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
        playing = false;
        gameObject.SetActive(false);
    }

    IEnumerator RevealRoutine(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetProgress(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }
        SetProgress(to);
    }

    IEnumerator FadeNarration(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            narration.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        narration.alpha = to;
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
    {
        if (g == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }
}
