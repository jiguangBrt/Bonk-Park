using UnityEngine;

// A drifting mote of light. When Lumi comes within absorb range it is pulled in and refills her
// energy — no precise contact needed. Drift is a slow Perlin-steered wander kept inside the view.
public class GlowMote : MonoBehaviour
{
    [Tooltip("Energy restored on absorb.")]
    [SerializeField] float energyValue = 20f;

    [Tooltip("Drift speed, m/s.")]
    [SerializeField] float driftSpeed = 0.6f;

    [Tooltip("How fast the drift direction wanders.")]
    [SerializeField] float wanderSpeed = 0.25f;

    [Tooltip("Lumi gets pulled in from this range, m.")]
    [SerializeField] float absorbRadius = 2.5f;

    [Tooltip("Homing speed at the moment of absorption, m/s.")]
    [SerializeField] float absorbSpeed = 9f;

    [Tooltip("Absorbed once this close, m.")]
    [SerializeField] float pickupRadius = 0.3f;

    [Tooltip("Ignore absorb radius and home to Lumi from any distance.")]
    [SerializeField] bool homesFromAnywhere = false;

    [Tooltip("Seconds the light bursts outward before it starts homing. 0 = home at once.")]
    [SerializeField] float burstDuration = 0f;

    [Tooltip("Outward speed at the start of the burst, m/s.")]
    [SerializeField] float burstSpeed = 6f;

    [Tooltip("How fast the burst coasts to a stop, m/s^2.")]
    [SerializeField] float burstDrag = 12f;

    [Tooltip("Chime played through Lumi when the mote is absorbed.")]
    [SerializeField] AudioClip collectSound;

    LumiEnergy lumi;
    Transform player;
    Vector2 arenaCenter;
    Vector2 arenaHalf;
    float noiseSeed;
    float burstRemaining;
    Vector2 burstVelocity;
    bool collected;

    void Awake()
    {
        noiseSeed = Random.value * 100f;
    }

    // Spawner hands the mote its target (Lumi) and the bounds it drifts inside.
    public void Init(LumiEnergy target, Vector2 center, Vector2 size)
    {
        lumi = target;
        player = target != null ? target.transform : null;
        arenaCenter = center;
        arenaHalf = size * 0.5f;
    }

    // Bonk light kicks outward, coasts to a stop, then falls through to homing.
    public void LaunchBurst(Vector2 direction)
    {
        burstRemaining = burstDuration;
        burstVelocity = direction.normalized * burstSpeed;
    }

    void FixedUpdate()
    {
        if (collected) return;

        Vector2 pos = transform.position;

        if (burstRemaining > 0f)
        {
            burstRemaining -= Time.fixedDeltaTime;
            burstVelocity = Vector2.MoveTowards(burstVelocity, Vector2.zero, burstDrag * Time.fixedDeltaTime);
            transform.position = ClampToArena(pos + burstVelocity * Time.fixedDeltaTime);
            return;
        }

        float dist = player != null ? Vector2.Distance(pos, player.position) : float.MaxValue;

        if (homesFromAnywhere || dist <= absorbRadius)
        {
            if (dist <= pickupRadius) { Collect(); return; }
            // Bonk light streaks straight in from anywhere; an idle mote eases in as it nears.
            float speed = homesFromAnywhere ? absorbSpeed : Mathf.Lerp(driftSpeed, absorbSpeed, 1f - dist / absorbRadius);
            transform.position = Vector2.MoveTowards(pos, player.position, speed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position = ClampToArena(pos + WanderDirection() * driftSpeed * Time.fixedDeltaTime);
        }
    }

    Vector2 WanderDirection()
    {
        float angle = Mathf.PerlinNoise(Time.time * wanderSpeed + noiseSeed, 0f) * Mathf.PI * 2f;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    Vector2 ClampToArena(Vector2 p)
    {
        p.x = Mathf.Clamp(p.x, arenaCenter.x - arenaHalf.x, arenaCenter.x + arenaHalf.x);
        p.y = Mathf.Clamp(p.y, arenaCenter.y - arenaHalf.y, arenaCenter.y + arenaHalf.y);
        return p;
    }

    void Collect()
    {
        collected = true;
        if (lumi != null)
        {
            lumi.Add(energyValue);
            if (collectSound != null)
            {
                var src = lumi.GetComponent<AudioSource>();
                if (src != null) src.PlayOneShot(collectSound);
            }
        }
        Destroy(gameObject);
    }
}
