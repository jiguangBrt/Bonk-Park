using System.Collections;
using UnityEngine;

// Background score for the whole game. A single instance survives scene loads, so the calm theme carries unbroken
// from the menu into the opening; a second copy loaded with the next scene hands off and removes itself. The score
// only ever crossfades between two ping-ponging sources, and a request for the theme already playing is ignored, so
// nothing restarts mid-track.
[DefaultExecutionOrder(-100)]
public class MusicManager : MonoBehaviour
{
    public enum Theme { None, Calm, Tension }

    [SerializeField] AudioClip calm;
    [SerializeField] AudioClip tension;

    [Tooltip("Playing volume of the calm theme (menu, story, death screen).")]
    [Range(0f, 1f)]
    [SerializeField] float calmVolume = 0.7f;

    [Tooltip("Playing volume of the tension theme (tutorial, chase, pause).")]
    [Range(0f, 1f)]
    [SerializeField] float tensionVolume = 0.55f;

    [Tooltip("Default crossfade length, seconds.")]
    [SerializeField] float crossfade = 1.5f;

    public static MusicManager Instance { get; private set; }
    public Theme Current { get; private set; }

    AudioSource a;
    AudioSource b;
    AudioSource active;
    Coroutine fading;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { a, b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.volume = 0f;
        }
        active = a;
    }

    public static void Play(Theme theme, float fade = -1f)
    {
        if (Instance != null) Instance.SetTheme(theme, fade);
    }

    public void SetTheme(Theme theme, float fadeTime = -1f)
    {
        if (theme == Current) return;
        Current = theme;

        AudioClip clip = theme == Theme.Calm ? calm : theme == Theme.Tension ? tension : null;
        float target = theme == Theme.Calm ? calmVolume : theme == Theme.Tension ? tensionVolume : 0f;
        if (fadeTime < 0f) fadeTime = crossfade;

        AudioSource next = active == a ? b : a;
        if (clip != null)
        {
            next.clip = clip;
            next.Play();
        }

        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(Crossfade(active, next, clip != null ? target : 0f, fadeTime));
        active = next;
    }

    // Unscaled time so the fade still runs while the game is paused (Time.timeScale == 0) or the death zoom is slowing it.
    IEnumerator Crossfade(AudioSource from, AudioSource to, float toVolume, float time)
    {
        float fromStart = from.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = time > 0f ? Mathf.Clamp01(t / time) : 1f;
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, toVolume, k);
            yield return null;
        }
        from.volume = 0f;
        if (from != to) from.Stop();
        to.volume = toVolume;
        fading = null;
    }
}
