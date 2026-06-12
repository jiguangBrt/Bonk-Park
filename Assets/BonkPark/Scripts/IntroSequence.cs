using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// First-run opening storyboard on a black screen. Each picture warms up from its centre outward; its lines glow in
// one at a time, each spoken by its own short clip, holding at full glow while the voice carries it and breathing for
// a beat on either side before the next. Between movements the picture dims and the next blooms; before the closing
// picture two red eyes surface in the dark. The final line hands control to the live game as the scene lights ignite.
// Any key skips straight to the handoff.
public class IntroSequence : MonoBehaviour
{
    [Header("Content")]

    [Tooltip("Story pictures, shown in order.")]
    [SerializeField] Sprite[] panels;

    [Tooltip("Narration lines, read in order and dealt out across the pictures by linesPerPanel.")]
    [TextArea] [SerializeField] string[] lines;

    [Tooltip("One spoken clip per line, in the same order as lines.")]
    [SerializeField] AudioClip[] lineClips;

    [Tooltip("How many narration lines each picture carries, in order; should sum to lines.Length.")]
    [SerializeField] int[] linesPerPanel = { 2, 2, 3, 2 };

    [Header("Picture, seconds")]

    [SerializeField] float revealIn = 1.5f;
    [SerializeField] float revealOut = 1.2f;

    [Tooltip("Per-pixel ramp width of the warm-up; larger = softer, slower swell.")]
    [SerializeField] float softness = 0.6f;

    [Header("Line pacing, seconds")]

    [SerializeField] float textFadeIn = 1.2f;
    [SerializeField] float textFadeOut = 1f;

    [Tooltip("Held breath after a line has appeared, before its voice speaks.")]
    [SerializeField] float voiceLead = 0.4f;

    [Tooltip("How long a line lingers at full glow once the voice has finished.")]
    [SerializeField] float breathAfter = 1f;

    [Tooltip("Silent rest on black between one line and the next.")]
    [SerializeField] float linePause = 1f;

    [Header("Firefly breath")]

    [Tooltip("Narration pulse speed; lower is slower.")]
    [SerializeField] float breathSpeed = 2.2f;

    [Tooltip("Narration pulse depth.")]
    [SerializeField] float breathAmount = 0.22f;

    [Header("Handoff, seconds")]

    [Tooltip("Picture index whose entrance the bat's eyes precede.")]
    [SerializeField] int eyesBeforePanel = 3;

    [SerializeField] float batBeat = 2.8f;

    [Tooltip("How long the scene lights swell up as control hands over.")]
    [SerializeField] float igniteDuration = 2.5f;

    [Header("Scene refs")]

    [SerializeField] PlayerController player;
    [SerializeField] MonoBehaviour batAI;
    [SerializeField] LumiEnergy lumi;
    [SerializeField] CompanionsSaved companions;
    [SerializeField] OnboardingSequence onboarding;
    [SerializeField] Image panelImage;
    [SerializeField] CanvasGroup rootGroup;
    [SerializeField] CanvasGroup batEyes;
    [SerializeField] TMP_Text narration;
    [SerializeField] AudioSource narrationSource;

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
        if (companions != null) companions.enabled = false;

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

    float Breath()
    {
        return 0.8f + breathAmount * Mathf.Sin(Time.time * breathSpeed);
    }

    IEnumerator PlayRoutine()
    {
        panelImage.sprite = panels[0];
        panelImage.color = Color.white;
        yield return RevealRoutine(0f, 1f, revealIn);

        int line = 0;
        for (int i = 0; i < panels.Length; i++)
        {
            int count = i < linesPerPanel.Length ? linesPerPanel[i] : 0;

            if (i > 0)
            {
                yield return TransitionRoutine(i);
                if (skipRequested) break;
            }

            for (int n = 0; n < count; n++)
            {
                yield return LineRoutine(line + n);
                if (skipRequested) break;
            }

            line += count;
            if (skipRequested) break;
        }

        yield return HandoffRoutine();
    }

    // Dim the picture and bloom the next. Before the closing picture the bat's eyes surface in the dark first.
    IEnumerator TransitionRoutine(int panel)
    {
        yield return RevealRoutine(currentProgress, 0f, revealOut);
        if (skipRequested) yield break;

        if (panel == eyesBeforePanel)
        {
            yield return BatEyesRoutine();
            if (skipRequested) yield break;
        }

        panelImage.sprite = panels[panel];
        panelImage.color = Color.white;
        yield return RevealRoutine(0f, 1f, revealIn);
    }

    // One line glows in, takes a breath, is spoken, lingers, then fades and rests before the next.
    IEnumerator LineRoutine(int index)
    {
        narration.text = index < lines.Length ? lines[index] : "";
        narration.alpha = 0f;

        float t = 0f;
        while (t < textFadeIn)
        {
            t += Time.deltaTime;
            narration.alpha = Mathf.Clamp01(t / textFadeIn) * Breath();
            if (skipRequested) yield break;
            yield return null;
        }

        yield return BreathHold(voiceLead);
        if (skipRequested) yield break;

        AudioClip clip = index < lineClips.Length ? lineClips[index] : null;
        if (narrationSource != null && clip != null)
        {
            narrationSource.clip = clip;
            narrationSource.Play();
            while (narrationSource.isPlaying)
            {
                narration.alpha = Breath();
                if (skipRequested) { narrationSource.Stop(); yield break; }
                yield return null;
            }
        }

        yield return BreathHold(breathAfter);
        if (skipRequested) yield break;

        yield return FadeNarration(narration.alpha, 0f, textFadeOut);
        yield return Rest(linePause);
    }

    // Two faint red eyes surface in the black before the closing picture.
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
        if (narrationSource != null) narrationSource.Stop();

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

        if (lumi != null)
        {
            lumi.enabled = true;
            // First run hands over to the tutorial, which brings Lumi up from dark; otherwise he arrives lit.
            if (onboarding != null) { lumi.SetEnergy(0f); lumi.DrainPaused = true; }
            lumi.Ignite(igniteDuration);
        }
        if (onboarding == null && player != null) player.enabled = true;

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

        // Hands-on tutorial before the chase. It sits inside the first-run handoff, so it only ever plays once.
        playing = false;
        if (onboarding != null) yield return onboarding.Run();

        // Scene is fully lit and the player is taught — now the tally and the chase begin together.
        if (companions != null) companions.enabled = true;
        if (batAI != null) batAI.enabled = true;

        PlayerPrefs.SetInt(SeenKey, 1);
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }

    IEnumerator BreathHold(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            narration.alpha = Breath();
            if (skipRequested) yield break;
            yield return null;
        }
    }

    IEnumerator Rest(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (skipRequested) yield break;
            yield return null;
        }
    }

    IEnumerator RevealRoutine(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetProgress(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
            if (skipRequested) break;
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
