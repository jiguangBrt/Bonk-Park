using TMPro;
using UnityEngine;

// The run's narrative tally: while Lumi survives, another firefly reaches the meadow every few seconds. Shows the
// running count and survival time in the corner and pops a quiet note in the park on each save. No score, no bonk count.
// Disabled during the opening (IntroSequence enables it at handoff) and switched off on death so the totals freeze.
public class CompanionsSaved : MonoBehaviour
{
    [Header("Cadence, seconds")]

    [Tooltip("Shortest gap between two fireflies reaching the meadow.")]
    [SerializeField] float saveIntervalMin = 5f;

    [Tooltip("Longest gap between two fireflies reaching the meadow.")]
    [SerializeField] float saveIntervalMax = 8f;

    [Header("Corner readout")]

    [Tooltip("HUD root, shown only while the tally is running (hidden during the opening).")]
    [SerializeField] GameObject hudRoot;

    [Tooltip("Survival time, mm:ss.")]
    [SerializeField] TMP_Text timeLabel;

    [Tooltip("Fireflies saved so far.")]
    [SerializeField] TMP_Text countLabel;

    [Header("Save note")]

    [Tooltip("World-space note spawned where each firefly slips away.")]
    [SerializeField] SavePopup popupPrefab;

    [Tooltip("Park, for keeping the note inside the view.")]
    [SerializeField] Park park;

    [Tooltip("Margin from the view edge for note placement, world units.")]
    [SerializeField] float popupMargin = 1.5f;

    int saved;
    float survivalTime;
    float nextSave;
    bool counting = true;

    public int Saved => saved;
    public float SurvivalTime => survivalTime;

    void OnEnable()
    {
        if (hudRoot != null) hudRoot.SetActive(true);
        nextSave = Random.Range(saveIntervalMin, saveIntervalMax);
        Refresh();
    }

    void OnDisable()
    {
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    void Update()
    {
        if (!counting) return;
        survivalTime += Time.deltaTime;
        nextSave -= Time.deltaTime;
        if (nextSave <= 0f)
        {
            saved++;
            nextSave = Random.Range(saveIntervalMin, saveIntervalMax);
            SpawnNote();
        }
        Refresh();
    }

    // Freeze the totals when the run ends, leaving the readout on screen at its final values.
    public void StopCounting()
    {
        counting = false;
    }

    void Refresh()
    {
        if (timeLabel != null)
        {
            int total = Mathf.FloorToInt(survivalTime);
            timeLabel.text = $"{total / 60:00}:{total % 60:00}";
        }
        if (countLabel != null) countLabel.text = saved.ToString();
    }

    void SpawnNote()
    {
        if (popupPrefab == null) return;
        Vector3 at = Vector3.zero;
        if (park != null)
        {
            Vector2 half = park.ReferenceViewport * 0.5f - Vector2.one * popupMargin;
            at = new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), 0f);
        }
        Instantiate(popupPrefab, at, Quaternion.identity);
    }
}
