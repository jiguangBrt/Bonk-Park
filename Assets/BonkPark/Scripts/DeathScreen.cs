using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The ending: how many fireflies sealed their light in time, laid over the final picture. Hidden until the run ends,
// then it fades in and stays up until the player chooses to fly again (which reloads the run; the opening is skipped
// from the second run on). Visibility runs through the CanvasGroup so the object stays active for the fade coroutine.
// Once shown, the lines breathe like the opening narration.
[RequireComponent(typeof(CanvasGroup))]
public class DeathScreen : MonoBehaviour
{
    [Tooltip("Lines shown over the picture, top to bottom. The line holding {0} is filled with the count.")]
    [SerializeField] TMP_Text[] lines;

    [Tooltip("Restarts the run.")]
    [SerializeField] Button playAgain;

    [Tooltip("Quits the game.")]
    [SerializeField] Button quit;

    [Tooltip("Fade-in, seconds.")]
    [SerializeField] float fadeIn = 0.8f;

    [Tooltip("Breathing pulse speed of the lines.")]
    [SerializeField] float breathSpeed = 1.8f;

    [Tooltip("Breathing pulse depth, fraction of full alpha.")]
    [SerializeField] float breathAmount = 0.18f;

    CanvasGroup group;
    bool breathing;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void Show(int saved)
    {
        foreach (var line in lines)
            if (line != null && line.text.Contains("{0}"))
                line.text = string.Format(line.text, saved);

        if (playAgain != null)
        {
            playAgain.onClick.RemoveAllListeners();
            playAgain.onClick.AddListener(Restart);
        }

        if (quit != null)
        {
            quit.onClick.RemoveAllListeners();
            quit.onClick.AddListener(Quit);
        }

        group.interactable = true;
        group.blocksRaycasts = true;
        StartCoroutine(FadeInRoutine());
    }

    void Update()
    {
        if (!breathing) return;
        float a = 1f - breathAmount + breathAmount * Mathf.Sin(Time.time * breathSpeed);
        foreach (var line in lines)
            if (line != null) line.alpha = a;
    }

    IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        group.alpha = 1f;
        breathing = true;
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
