using UnityEngine;

// Night ambience for the chase: a cricket chirp drifts in at random gaps while the tension score plays, and stays
// quiet under the calm menu, the storybook, and the death screen. Pauses with the game.
public class AmbientCrickets : MonoBehaviour
{
    [SerializeField] AudioClip chirp;

    [Range(0f, 1f)]
    [SerializeField] float volume = 0.35f;

    [Tooltip("Shortest gap between chirps, seconds.")]
    [SerializeField] float minGap = 10f;

    [Tooltip("Longest gap between chirps, seconds.")]
    [SerializeField] float maxGap = 22f;

    AudioSource source;
    float timer;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        timer = Random.Range(minGap, maxGap);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (chirp == null || MusicManager.Instance == null || MusicManager.Instance.Current != MusicManager.Theme.Tension) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        source.PlayOneShot(chirp, volume);
        timer = Random.Range(minGap, maxGap);
    }
}
