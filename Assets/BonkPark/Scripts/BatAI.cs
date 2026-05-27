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

    static readonly int BonkTriggerId = Animator.StringToHash("Bonk");

    // Populates the two curves with sensible defaults on inspector Reset.
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
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        var mainCam = Camera.main;
        if (mainCam != null) cameraShake = mainCam.GetComponent<CameraShake>();
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.right;
        currentSpeed = 0f;

        int capacity = Mathf.CeilToInt(MaxReactionDelay / Time.fixedDeltaTime) + 8;
        desiredBuffer = new Vector2[capacity];
        desiredHead = 0;
        desiredCount = 0;
    }

    // Per-physics-step steering toward Lumi's current position; desired direction is read from a delay-shifted buffer so direction changes take reactionDelay seconds to register.
    void FixedUpdate()
    {
        if (stunRemaining > 0f)
        {
            stunRemaining -= Time.fixedDeltaTime;
            rb.velocity = Vector2.zero;
            currentSpeed = 0f;
            return;
        }

        if (target == null) return;

        Vector2 toTarget = (Vector2)target.position - rb.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector2 instantDesired = toTarget.normalized;

        desiredBuffer[desiredHead] = instantDesired;
        desiredHead = (desiredHead + 1) % desiredBuffer.Length;
        if (desiredCount < desiredBuffer.Length) desiredCount++;

        int stepsAgo = Mathf.Min(Mathf.RoundToInt(reactionDelay / Time.fixedDeltaTime), desiredCount - 1);
        int readIdx = (desiredHead - 1 - stepsAgo + desiredBuffer.Length) % desiredBuffer.Length;
        Vector2 desired = desiredBuffer[readIdx];

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

        if (flipSpriteByHeading && sr != null && Mathf.Abs(heading.x) > 0.01f)
            sr.flipX = heading.x < 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var death = collision.gameObject.GetComponent<PlayerDeath>();
        if (death != null) { death.Die(); return; }

        var bonk = collision.gameObject.GetComponent<Bonkable>();
        if (bonk != null)
        {
            stunRemaining = bonk.StunDuration;
            rb.velocity = Vector2.zero;
            currentSpeed = 0f;
            if (animator != null) animator.SetTrigger(BonkTriggerId);
            if (cameraShake != null) cameraShake.Shake(shakeDuration, shakeMagnitude);
        }
    }
}
