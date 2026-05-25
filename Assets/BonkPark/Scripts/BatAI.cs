using UnityEngine;

// Bat chase AI: reads a delayed snapshot of Lumi's position from ReactionBuffer and steers toward it with alignment-shaped speed.
[RequireComponent(typeof(Rigidbody2D))]
public class BatAI : MonoBehaviour
{
    [Header("Target")]

    [Tooltip("Lumi's reaction buffer.")]
    [SerializeField] ReactionBuffer target;

    [Tooltip("Reaction delay, seconds.")]
    [Range(0.05f, 0.8f)]
    [SerializeField] float reactionDelay = 0.4f;

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
    }

    // Per-physics-step steering toward the delayed target snapshot.
    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 snapshot = target.GetSnapshot(reactionDelay);
        Vector2 toTarget = snapshot - rb.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector2 desired = toTarget.normalized;

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
        if (death != null) death.Die();
    }
}
