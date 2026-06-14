using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Esc freezes the world behind a frosted sheet and offers to carry on or step out. The frost is the frozen frame
// halved down a pyramid and tented back up, so the repeated bilinear taps soften it evenly into glass. Stepping out
// asks first so a run is never thrown away by a stray click.
public class PauseMenu : MonoBehaviour
{
    [SerializeField] Camera sourceCamera;
    [SerializeField] CanvasGroup panel;
    [SerializeField] RawImage frost;
    [SerializeField] GameObject confirm;
    [SerializeField] Button continueButton;
    [SerializeField] Button returnButton;
    [SerializeField] Button confirmYes;
    [SerializeField] Button confirmNo;

    [Header("Frozen while paused")]
    [SerializeField] PlayerController player;
    [SerializeField] LumiEnergy lumi;

    [Tooltip("How many times the frozen frame is halved and rebuilt; more steps read as a softer, more even frost.")]
    [SerializeField] int blurSteps = 3;

    [SerializeField] string menuScene = "MainMenu";

    RenderTexture shot;
    bool paused;

    void Awake()
    {
        Show(false);
        if (confirm != null) confirm.SetActive(false);

        if (continueButton != null) continueButton.onClick.AddListener(Resume);
        if (returnButton != null) returnButton.onClick.AddListener(AskReturn);
        if (confirmYes != null) confirmYes.onClick.AddListener(ReturnToMenu);
        if (confirmNo != null) confirmNo.onClick.AddListener(CancelReturn);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (!paused) Pause();
        else if (confirm != null && confirm.activeSelf) CancelReturn();
        else Resume();
    }

    void Pause()
    {
        paused = true;
        if (confirm != null) confirm.SetActive(false);
        ShowChoices(true);
        CaptureFrost();
        Time.timeScale = 0f;
        if (player != null) player.enabled = false;
        if (lumi != null) lumi.DrainPaused = true;
        Show(true);
    }

    void Resume()
    {
        paused = false;
        if (confirm != null) confirm.SetActive(false);
        Time.timeScale = 1f;
        if (player != null) player.enabled = true;
        if (lumi != null) lumi.DrainPaused = false;
        Show(false);
        Release();
    }

    void AskReturn()
    {
        ShowChoices(false);
        if (confirm != null) confirm.SetActive(true);
    }

    void CancelReturn()
    {
        if (confirm != null) confirm.SetActive(false);
        ShowChoices(true);
    }

    void ShowChoices(bool on)
    {
        if (continueButton != null) continueButton.gameObject.SetActive(on);
        if (returnButton != null) returnButton.gameObject.SetActive(on);
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        Release();
        SceneManager.LoadScene(menuScene);
    }

    void Show(bool on)
    {
        if (panel == null) return;
        panel.alpha = on ? 1f : 0f;
        panel.interactable = on;
        panel.blocksRaycasts = on;
    }

    void CaptureFrost()
    {
        if (sourceCamera == null || frost == null) return;
        Release();

        int w = Mathf.Max(1, Screen.width / 2);
        int h = Mathf.Max(1, Screen.height / 2);
        var capture = RenderTexture.GetTemporary(w, h, 16);
        capture.filterMode = FilterMode.Bilinear;

        RenderTexture prev = sourceCamera.targetTexture;
        sourceCamera.targetTexture = capture;
        sourceCamera.Render();
        sourceCamera.targetTexture = prev;

        shot = Soften(capture);
        RenderTexture.ReleaseTemporary(capture);
        frost.texture = shot;
    }

    // Halve the frame down a pyramid, then tent it back up. Each bilinear step averages a wider neighbourhood than a
    // single stretch can, so the result reads as smooth frosted glass rather than a pixelated shrink.
    RenderTexture Soften(RenderTexture src)
    {
        int steps = Mathf.Max(1, blurSteps);
        var down = new RenderTexture[steps];
        RenderTexture cur = src;
        int w = src.width, h = src.height;
        for (int i = 0; i < steps; i++)
        {
            w = Mathf.Max(1, w / 2);
            h = Mathf.Max(1, h / 2);
            down[i] = RenderTexture.GetTemporary(w, h, 0);
            down[i].filterMode = FilterMode.Bilinear;
            Graphics.Blit(cur, down[i]);
            cur = down[i];
        }
        for (int i = steps - 2; i >= 0; i--)
        {
            Graphics.Blit(cur, down[i]);
            cur = down[i];
        }
        for (int i = 1; i < steps; i++) RenderTexture.ReleaseTemporary(down[i]);
        return down[0];
    }

    void Release()
    {
        if (shot != null) { RenderTexture.ReleaseTemporary(shot); shot = null; }
    }
}
