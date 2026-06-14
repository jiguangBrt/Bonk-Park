using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The front door. A player who has never seen the opening is sent straight into it and never lands here; everyone
// after does. Start drops into the park, Story replays the opening as a re-watch, Quit leaves.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] Button startGame;
    [SerializeField] Button story;
    [SerializeField] Button quit;

    [Tooltip("Scene that holds the actual game.")]
    [SerializeField] string gameScene = "MainScene";

    void Awake()
    {
        MusicManager.Play(MusicManager.Theme.Calm);

        if (PlayerPrefs.GetInt(IntroSequence.SeenKey, 0) == 0)
        {
            SceneManager.LoadScene(gameScene);
            return;
        }

        if (startGame != null) startGame.onClick.AddListener(StartGame);
        if (story != null) story.onClick.AddListener(ReplayStory);
        if (quit != null) quit.onClick.AddListener(Quit);
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameScene);
    }

    void ReplayStory()
    {
        IntroSequence.ForceReplay = true;
        SceneManager.LoadScene(gameScene);
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
