using System.Collections;
using UnityEngine;

// Trickles glow motes into view: one every few seconds, up to a cap. When Lumi absorbs one the
// count drops and the next tick tops it back up, so the refuel economy stays sparse but never empty.
public class GlowMoteSpawner : MonoBehaviour
{
    [Tooltip("Mote prefab to spawn.")]
    [SerializeField] GlowMote motePrefab;

    [Tooltip("Park that defines the camera view bounds.")]
    [SerializeField] Park park;

    [Tooltip("Lumi's energy — motes refill it and home toward it.")]
    [SerializeField] LumiEnergy lumi;

    [Tooltip("Most motes alive at once.")]
    [SerializeField] int population = 3;

    [Tooltip("Seconds between spawns.")]
    [SerializeField] float spawnInterval = 5f;

    [Tooltip("Keep motes this far inside the screen edge, m.")]
    [SerializeField] float edgeMargin = 1f;

    [Tooltip("Keep spawns at least this far from Lumi, m.")]
    [SerializeField] float playerClearance = 2f;

    [Tooltip("Keep spawns at least this far from each other, m.")]
    [SerializeField] float minMoteSpacing = 5f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (FindObjectsOfType<GlowMote>().Length < population) SpawnMote();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnMote()
    {
        Vector2 pos = PickSpawnPoint();
        var mote = Instantiate(motePrefab, pos, Quaternion.identity);
        mote.Init(lumi, park.transform.position, BoundsHalf() * 2f);
    }

    // Half-extent of the spawn/drift area: the camera view, pulled in by the edge margin so a
    // mote never drifts off screen.
    Vector2 BoundsHalf()
    {
        return park.ReferenceViewport * 0.5f - Vector2.one * edgeMargin;
    }

    // Random point in view, re-rolled until it clears Lumi and the other motes (or attempts run out).
    Vector2 PickSpawnPoint()
    {
        Vector2 center = park.transform.position;
        Vector2 half = BoundsHalf();
        var others = FindObjectsOfType<GlowMote>();
        Vector2 pos = center;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            pos = center + new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
            if (!TooClose(pos, others)) break;
        }
        return pos;
    }

    bool TooClose(Vector2 pos, GlowMote[] others)
    {
        if (lumi != null && ((Vector2)lumi.transform.position - pos).sqrMagnitude < playerClearance * playerClearance) return true;
        foreach (var mote in others)
        {
            if (mote == null) continue;
            if (((Vector2)mote.transform.position - pos).sqrMagnitude < minMoteSpacing * minMoteSpacing) return true;
        }
        return false;
    }
}
