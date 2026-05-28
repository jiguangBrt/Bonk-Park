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

    [Tooltip("Obstacle lookahead distance, m. Sets the reaction window for the bonk mechanic.")]
    [SerializeField] float dangerLookahead = 2.5f;

    [Tooltip("Danger falloff exponent. Higher = only very-close obstacles repel.")]
    [SerializeField] float dangerFalloff = 2f;

    [Tooltip("Weight of danger vs interest. Higher = more avoidance, lower = more bonking.")]
    [SerializeField] float dangerWeight = 1.5f;

    [Tooltip("Bonus for slots near current heading; kills jitter between adjacent slots.")]
    [Range(0f, 0.5f)]
    [SerializeField] float headingMomentumBias = 0.15f;

    [Header("Sprite")]

    [Tooltip("Flip sprite by heading.")]
    [SerializeField] bool flipSpriteByHeading = true;

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

    [Tooltip("Deceleration while sliding through stun, m/s^2. Tuned so the slide stops near stun end.")]
    [SerializeField] float stunDeceleration = 5f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator animator;
    CameraShake cameraShake;
    Vector2 heading;
    float currentSpeed;

    Vector2[] desiredBuffer;
    int desiredHead;
    int desiredCount;

    float stunRemaining;
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
        UpdateSpriteFacing();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (TryKillPlayerOnContact(collision)) return;
        TryBonk(collision);
    }

    void CacheComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
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

    // Context steering: score each evenly-spaced slot by interest (toward delayed desired, plus a heading-momentum bonus) minus danger (raycast proximity). Best-scoring slot becomes the new desired direction.
    Vector2 ChooseDesiredDirection(Vector2 delayedDesired)
    {
        Vector2 best = delayedDesired;
        float bestScore = float.NegativeInfinity;
        float twoPi = Mathf.PI * 2f;

        for (int i = 0; i < directionSlots; i++)
        {
            float a = (i / (float)directionSlots) * twoPi;
            Vector2 slot = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            float interest = Mathf.Max(0f, Vector2.Dot(slot, delayedDesired))
                           + headingMomentumBias * Mathf.Max(0f, Vector2.Dot(slot, heading));

            float score = interest - ScoreDanger(slot) * dangerWeight;
            if (score > bestScore)
            {
                bestScore = score;
                best = slot;
            }
        }
        return best;
    }

    float ScoreDanger(Vector2 slot)
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, slot, dangerLookahead, obstacleMask);
        if (hit.collider == null) return 0f;
        float proximity = 1f - hit.distance / dangerLookahead;
        return Mathf.Pow(proximity, dangerFalloff);
    }

    void ApplyMotion(Vector2 desired)
    {
        // Alignment scales target speed: aligned = full, sideways = slow for tighter turn, reversed = near zero.
        float alignment = Vector2.Dot(heading, desired);
        float targetSpeed = maxSpeed * speedByAlignment.Evaluate(alignment);

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

    void UpdateSpriteFacing()
    {
        if (!flipSpriteByHeading || sr == null) return;
        if (Mathf.Abs(heading.x) <= 0.01f) return;
        sr.flipX = heading.x < 0f;
    }

    bool TryKillPlayerOnContact(Collision2D collision)
    {
        var death = collision.gameObject.GetComponent<PlayerDeath>();
        if (death == null) return false;
        death.Die();
        return true;
    }

    // Reflect pre-impact velocity off the contact normal and damp it; flipping heading is the key to breaking the stun-rebonk loop.
    void TryBonk(Collision2D collision)
    {
        var bonk = collision.gameObject.GetComponent<Bonkable>();
        if (bonk == null) return;

        Vector2 normal = collision.GetContact(0).normal;
        Vector2 reflected = Vector2.Reflect(lastVelocity, normal) * bonkBounceRetention;

        rb.velocity = reflected;
        currentSpeed = reflected.magnitude;
        if (reflected.sqrMagnitude > 0.0001f) heading = reflected.normalized;
        lastVelocity = reflected;

        stunRemaining = bonk.StunDuration;
        if (animator != null) animator.SetTrigger(BonkTriggerId);
        if (cameraShake != null) cameraShake.Shake(shakeDuration, shakeMagnitude);
    }
}
