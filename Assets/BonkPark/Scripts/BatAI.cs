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

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 heading;
    float currentSpeed;

    Vector2[] desiredBuffer;
    int desiredHead;
    int desiredCount;

    float stunRemaining;

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
        // While stunned, leave velocity alone so the collision impulse carries through as knockback.
        if (stunRemaining > 0f)
        {
            stunRemaining -= Time.fixedDeltaTime;
            currentSpeed = rb.velocity.magnitude;
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
        if (bonk != null) stunRemaining = bonk.StunDuration;
    }
}
