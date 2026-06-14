using UnityEngine;

// Bat chase AI: steers toward Lumi's current position with a delayed reaction to direction changes.
[RequireComponent(typeof(Rigidbody2D))]
public class BatAI : MonoBehaviour
{
    [Header("Target")]

    [Tooltip("Lumi's transform.")]
    [SerializeField] Transform target;

    [Tooltip("Steering reaction delay, seconds.")]
    [Range(0.05f, 0.3f)]
    [SerializeField] float reactionDelay = 0.1f;

    const float MaxReactionDelay = 0.3f;

    [Header("Movement")]

    [Tooltip("Max speed, m/s.")]
    [SerializeField] float maxSpeed = 13f;

    [Tooltip("Acceleration, m/s^2.")]
    [SerializeField] float acceleration = 12f;

    [Tooltip("Auto-brake, m/s^2.")]
    [SerializeField] float autoBraking = 25f;

    [Tooltip("Alignment to speed factor.")]
    [SerializeField] AnimationCurve speedByAlignment;

    [Tooltip("Speed to turn rate.")]
    [SerializeField] AnimationCurve turnRateBySpeed;

    [Header("Context Steering")]

    [Tooltip("Layers treated as obstacles for danger raycasts.")]
    [SerializeField] LayerMask obstacleMask;

    [Tooltip("Number of evenly-spaced sample directions; higher = smoother but more raycasts.")]
    [Range(8, 32)]
    [SerializeField] int directionSlots = 16;

    [Tooltip("Lookahead straight ahead, m. The bat sees obstacles in front from this far.")]
    [SerializeField] float frontLookahead = 6f;

    [Tooltip("Lookahead to the sides, m. Shorter so a wall the bat only slides past doesn't scare it.")]
    [SerializeField] float sideLookahead = 3f;

    [Tooltip("Danger probe radius, m. Match the bat's collider half-width so it won't steer where its body can't fit.")]
    [SerializeField] float bodyRadius = 0.6f;

    [Tooltip("Danger falloff exponent. Higher = only very-close obstacles repel.")]
    [SerializeField] float dangerFalloff = 2f;

    [Tooltip("How hard danger suppresses interest. Higher = more avoidance, lower = more bonking.")]
    [SerializeField] float dangerWeight = 1f;

    [Tooltip("Bonus for slots near current heading; kills jitter between adjacent slots.")]
    [Range(0f, 0.5f)]
    [SerializeField] float headingMomentumBias = 0.15f;

    [Tooltip("Small lean toward the clearer side when a wall sits dead ahead.")]
    [Range(0f, 0.5f)]
    [SerializeField] float sidePref = 0.15f;

    [Header("Init")]

    [Tooltip("Initial heading.")]
    [SerializeField] Vector2 initialHeading = Vector2.right;

    [Header("Bonk")]

    [Tooltip("Camera shake duration on bonk, seconds.")]
    [SerializeField] float shakeDuration = 0.2f;

    [Tooltip("Camera shake magnitude, world units.")]
    [SerializeField] float shakeMagnitude = 0.15f;

    [Tooltip("Bounce-back velocity fraction after a bonk (0.25-0.35 feels right).")]
    [Range(0f, 1f)]
    [SerializeField] float bonkBounceRetention = 0.3f;

    [Tooltip("Lowest push-off speed after a bonk, m/s, so a slow hit still slides clear of the wall.")]
    [SerializeField] float bonkEscapeSpeed = 4f;

    [Tooltip("Deceleration while sliding through stun, m/s^2. Tuned so the slide stops near stun end.")]
    [SerializeField] float stunDeceleration = 5f;

    [Tooltip("Spawner that spills light at the bonk spot.")]
    [SerializeField] GlowMoteSpawner lightSpawner;

    [Tooltip("Impact played when the bat hits an obstacle.")]
    [SerializeField] AudioClip bonkSound;

    [Tooltip("Seconds skipped at the head of the impact clip so the strike lands the instant of contact.")]
    [SerializeField] float bonkSoundLead;

    [Header("Debug")]

    [Tooltip("Draw steering rays and the heading/desired velocity arrows in the editor.")]
    [SerializeField] bool drawDebugGizmos;

    [Tooltip("Meters drawn per m/s for the velocity arrows.")]
    [SerializeField] float debugVelocityScale = 0.3f;

    Rigidbody2D rb;
    Animator animator;
    AudioSource sfx;
    CameraShake cameraShake;
    Vector2 heading;
    float currentSpeed;

    // Last frame's chosen direction and target speed, cached for the debug gizmos.
    Vector2 debugDesired;
    float debugTargetSpeed;

    Vector2[] desiredBuffer;
    int desiredHead;
    int desiredCount;

    float[] slotScore;
    float[] slotDanger;

    float stunRemaining;
    int bushCount;
    float bushSlow = 1f;
    // OnCollisionEnter2D fires after the physics solver has already zeroed rb.velocity, so the reflection has to use the velocity from the previous step.
    Vector2 lastVelocity;

    static readonly int BonkTriggerId = Animator.StringToHash("Bonk");

    // Populates the curves and obstacle mask with sensible defaults on inspector Reset.
    void Reset()
    {
        speedByAlignment = new AnimationCurve(
            new Keyframe(-1f, 0.1f),
            new Keyframe(0f, 0.35f),
            new Keyframe(1f, 1f)
        );
        turnRateBySpeed = new AnimationCurve(
            new Keyframe(0f, 360f),
            new Keyframe(0.5f, 200f),
            new Keyframe(1f, 120f)
        );
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0) obstacleMask = 1 << obstacleLayer;
    }

    void Awake()
    {
        CacheComponents();
        InitState();
        InitIntentBuffer();
    }

    void FixedUpdate()
    {
        if (stunRemaining > 0f)
        {
            TickStunSlide();
            return;
        }

        if (!TryGetToTarget(out Vector2 toTarget)) return;

        Vector2 delayedDesired = PushIntentAndReadDelayed(toTarget.normalized);
        Vector2 desired = ChooseDesiredDirection(delayedDesired);
        ApplyMotion(desired);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryKillPlayerOnContact(collision)) return;
        TryBonk(collision);
    }

    void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sfx = GetComponent<AudioSource>();
        var mainCam = Camera.main;
        if (mainCam != null) cameraShake = mainCam.GetComponent<CameraShake>();
    }

    void InitState()
    {
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.right;
        currentSpeed = 0f;
    }

    // Sized to hold MaxReactionDelay worth of FixedUpdate steps plus margin, so reactionDelay can be raised in the inspector without overrunning.
    void InitIntentBuffer()
    {
        int capacity = Mathf.CeilToInt(MaxReactionDelay / Time.fixedDeltaTime) + 8;
        desiredBuffer = new Vector2[capacity];
        desiredHead = 0;
        desiredCount = 0;
    }

    // Stun slide: keep moving along the reflected heading but decelerate so the bat ends the stun in a fresh position with heading already pointing away from the wall.
    void TickStunSlide()
    {
        stunRemaining -= Time.fixedDeltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, stunDeceleration * Time.fixedDeltaTime);
        rb.velocity = heading * currentSpeed;
        lastVelocity = rb.velocity;
    }

    bool TryGetToTarget(out Vector2 toTarget)
    {
        toTarget = default;
        if (target == null) return false;
        toTarget = (Vector2)target.position - rb.position;
        return toTarget.sqrMagnitude >= 0.0001f;
    }

    // Push this step's instant-desired into the ring buffer, then read out the entry 'reactionDelay' worth of steps back so direction changes lag the bat by reactionDelay seconds.
    Vector2 PushIntentAndReadDelayed(Vector2 instantDesired)
    {
        desiredBuffer[desiredHead] = instantDesired;
        desiredHead = (desiredHead + 1) % desiredBuffer.Length;
        if (desiredCount < desiredBuffer.Length) desiredCount++;

        int stepsAgo = Mathf.Min(Mathf.RoundToInt(reactionDelay / Time.fixedDeltaTime), desiredCount - 1);
        int readIdx = (desiredHead - 1 - stepsAgo + desiredBuffer.Length) % desiredBuffer.Length;
        return desiredBuffer[readIdx];
    }

    // Context steering: score each slot by interest (toward the delayed desired plus a heading bonus), masked by how dangerous that slot is. The best slot wins and gets interpolated against its neighbours so the heading isn't locked to the discrete slots.
    Vector2 ChooseDesiredDirection(Vector2 delayedDesired)
    {
        int n = directionSlots;
        if (slotScore == null || slotScore.Length != n)
        {
            slotScore = new float[n];
            slotDanger = new float[n];
        }

        float twoPi = Mathf.PI * 2f;
        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);

        int best = 0;
        float bestScore = float.NegativeInfinity;
        int wantMost = 0;
        float mostInterest = float.NegativeInfinity;

        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * twoPi;
            Vector2 slot = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            // See far ahead, short to the sides, and a little further the faster we're already committed.
            float fwd = Mathf.Clamp01(Vector2.Dot(slot, heading));
            float reach = Mathf.Lerp(sideLookahead, frontLookahead * Mathf.Lerp(0.5f, 1f, speedT), fwd);

            float interest = Mathf.Max(0f, Vector2.Dot(slot, delayedDesired))
                           + headingMomentumBias * Mathf.Max(0f, Vector2.Dot(slot, heading));
            float danger = ScoreDanger(slot, reach);

            slotScore[i] = interest * Mathf.Max(0f, 1f - dangerWeight * danger);
            slotDanger[i] = danger;

            if (slotScore[i] > bestScore) { bestScore = slotScore[i]; best = i; }
            if (interest > mostInterest) { mostInterest = interest; wantMost = i; }
        }

        // Every way toward Lumi is walled off: head for her anyway and let the bonk recovery deal with the wall.
        if (bestScore <= 0f) best = wantMost;

        int left = (best - 1 + n) % n;
        int right = (best + 1) % n;
        float sL = slotScore[left], s0 = slotScore[best], sR = slotScore[right];

        // Parabola vertex across the winning slot and its neighbours, so the heading lands between slots.
        float offset = 0f;
        float denom = sL - 2f * s0 + sR;
        if (denom < -1e-4f) offset = 0.5f * (sL - sR) / denom;

        // Lean toward whichever neighbour is clearer; Sign is 0 on a perfectly symmetric wall so momentum decides instead.
        offset += sidePref * Mathf.Sign(slotDanger[left] - slotDanger[right]);
        offset = Mathf.Clamp(offset, -0.5f, 0.5f);

        float angle = ((best + offset) / n) * twoPi;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // Sweep a body-width circle instead of a thin ray so a slot the centre could thread but the body can't gets flagged as dangerous.
    float ScoreDanger(Vector2 slot, float reach)
    {
        RaycastHit2D hit = Physics2D.CircleCast(rb.position, bodyRadius, slot, reach, obstacleMask);
        if (hit.collider == null) return 0f;
        if (hit.distance <= 0f) return 1f;   // already overlapping that side: the body can't leave this way
        float proximity = 1f - hit.distance / reach;
        return Mathf.Pow(proximity, dangerFalloff);
    }

    void ApplyMotion(Vector2 desired)
    {
        // Alignment scales target speed: aligned = full, sideways = slow for tighter turn, reversed = near zero.
        float alignment = Vector2.Dot(heading, desired);
        float targetSpeed = maxSpeed * speedByAlignment.Evaluate(alignment) * BushSpeedMultiplier();

        debugDesired = desired;
        debugTargetSpeed = targetSpeed;

        // Asymmetric ramp combined with alignment falloff produces a natural drift on sharp turns.
        float rate = targetSpeed > currentSpeed ? acceleration : autoBraking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // Turn rate falls with speed: low speed = tight radius, high speed = large radius.
        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);
        float turnRate = turnRateBySpeed.Evaluate(speedT);
        float maxRadians = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        heading = Vector3.RotateTowards(heading, desired, maxRadians, 0f);

        rb.velocity = heading * currentSpeed;
        lastVelocity = rb.velocity;
    }

    // A bush drags on the bat while it's inside; the bush sets the factor, the count handles overlap.
    public void EnterBush(float multiplier) { bushSlow = multiplier; bushCount++; }
    public void ExitBush() => bushCount = Mathf.Max(0, bushCount - 1);

    float BushSpeedMultiplier() => bushCount > 0 ? bushSlow : 1f;

    bool TryKillPlayerOnContact(Collision2D collision)
    {
        var death = collision.gameObject.GetComponent<PlayerDeath>();
        if (death == null) return false;
        death.Die();
        return true;
    }

    // Force the heading away from the wall and guarantee a minimum push-off, so even a slow bonk slides clear instead of scraping the same wall over and over.
    void TryBonk(Collision2D collision)
    {
        var bonk = collision.gameObject.GetComponent<Bonkable>();
        if (bonk == null) return;

        ContactPoint2D contact = collision.GetContact(0);

        Vector2 escape = -lastVelocity.normalized;
        if (Vector2.Dot(escape, contact.normal) <= 0f) escape = contact.normal;
        heading = escape;
        currentSpeed = Mathf.Max(lastVelocity.magnitude * bonkBounceRetention, bonkEscapeSpeed);

        rb.velocity = heading * currentSpeed;
        lastVelocity = rb.velocity;

        stunRemaining = bonk.StunDuration;
        if (animator != null) animator.SetTrigger(BonkTriggerId);
        PlayBonkSound();
        if (cameraShake != null) cameraShake.Shake(shakeDuration, shakeMagnitude);
        if (lightSpawner != null) lightSpawner.SpawnBonkLight(contact.point);
        bonk.OnBonk();
    }

    // Start past the clip's silent head so the strike reads on contact, not a beat later. clip+time+Play instead of
    // PlayOneShot because only a positioned source can skip the lead-in.
    void PlayBonkSound()
    {
        if (sfx == null || bonkSound == null) return;
        sfx.clip = bonkSound;
        sfx.time = Mathf.Clamp(bonkSoundLead, 0f, Mathf.Max(0f, bonkSound.length - 0.01f));
        sfx.Play();
    }

    void OnDrawGizmos()
    {
        if (!drawDebugGizmos) return;

        Vector2 origin = Application.isPlaying && rb != null ? rb.position : (Vector2)transform.position;
        float twoPi = Mathf.PI * 2f;

        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);

        // Steering slots: sweep a body-width circle per slot; clip the line where the body would first touch.
        for (int i = 0; i < directionSlots; i++)
        {
            float a = (i / (float)directionSlots) * twoPi;
            Vector2 slot = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float fwd = Mathf.Clamp01(Vector2.Dot(slot, heading));
            float reach = Mathf.Lerp(sideLookahead, frontLookahead * Mathf.Lerp(0.5f, 1f, speedT), fwd);
            RaycastHit2D hit = Physics2D.CircleCast(origin, bodyRadius, slot, reach, obstacleMask);
            if (hit.collider == null)
            {
                Gizmos.color = new Color(0.3f, 0.6f, 0.3f, 0.5f);
                Gizmos.DrawLine(origin, origin + slot * reach);
            }
            else if (hit.distance <= 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(origin, bodyRadius);
            }
            else
            {
                Vector2 stop = origin + slot * hit.distance;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, stop);
                Gizmos.DrawWireSphere(stop, bodyRadius);
            }
        }

        // Current heading scaled by current speed.
        Gizmos.color = Color.cyan;
        DrawArrow(origin, heading * currentSpeed * debugVelocityScale);

        // Chosen direction scaled by target speed (only set while playing).
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            DrawArrow(origin, debugDesired * debugTargetSpeed * debugVelocityScale);
        }
    }

    static void DrawArrow(Vector2 from, Vector2 vec)
    {
        Vector2 tip = from + vec;
        Gizmos.DrawLine(from, tip);
        if (vec.sqrMagnitude < 0.0001f) return;
        Vector2 dir = vec.normalized;
        Vector2 wing = -dir * 0.3f;
        Vector2 perp = new Vector2(-dir.y, dir.x) * 0.15f;
        Gizmos.DrawLine(tip, tip + wing + perp);
        Gizmos.DrawLine(tip, tip + wing - perp);
    }
}
