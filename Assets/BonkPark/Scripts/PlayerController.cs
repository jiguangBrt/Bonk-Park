using UnityEngine;

// Mouse-driven cruise controller for Lumi: mouse position is the target, distance and heading alignment shape the speed.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]

    [Tooltip("Max speed, m/s.")]
    [SerializeField] float maxSpeed = 12f;

    [Tooltip("Slowdown start distance, m.")]
    [SerializeField] float arrivalRange = 2.5f;

    [Tooltip("Acceleration, m/s^2.")]
    [SerializeField] float acceleration = 15f;

    [Tooltip("Auto-brake, m/s^2.")]
    [SerializeField] float autoBraking = 25f;

    [Tooltip("Park-mode enter radius, m.")]
    [SerializeField] float stopRadius = 0.4f;

    [Tooltip("Park-mode exit radius, m.")]
    [SerializeField] float resumeRadius = 1.0f;

    [Tooltip("Distance to speed factor.")]
    [SerializeField] AnimationCurve targetSpeedByDistance = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Alignment to speed factor.")]
    [SerializeField] AnimationCurve speedByAlignment;

    [Header("Turning")]

    [Tooltip("Speed to turn rate.")]
    [SerializeField] AnimationCurve turnRateBySpeed;

    [Tooltip("Sprite heading offset, deg.")]
    [SerializeField] float spriteHeadOffsetDeg = -90f;

    [Header("Init")]

    [Tooltip("Initial heading.")]
    [SerializeField] Vector2 initialHeading = Vector2.up;

    [Tooltip("Initial speed, m/s.")]
    [SerializeField] float initialSpeed = 0f;

    Rigidbody2D rb;
    Vector2 heading;
    float currentSpeed;
    bool parked;

    // Populates the two curves with sensible defaults on inspector Reset.
    void Reset()
    {
        targetSpeedByDistance = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        speedByAlignment = new AnimationCurve(
            new Keyframe(-1f, 0.2f),
            new Keyframe(0f, 0.5f),
            new Keyframe(1f, 1f)
        );
        turnRateBySpeed = new AnimationCurve(
            new Keyframe(0f, 540f),
            new Keyframe(0.5f, 240f),
            new Keyframe(1f, 180f)
        );
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.up;
        currentSpeed = Mathf.Clamp(initialSpeed, 0f, maxSpeed);
    }

    // Per-physics-step steering and speed update.
    void FixedUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toCursor = mouseWorld - rb.position;
        float distance = toCursor.magnitude;

        // Parked state uses distance hysteresis plus a speed gate so high-speed overshoot doesn't leave us limping.
        if (parked)
        {
            if (currentSpeed < 0.01f && distance > resumeRadius) parked = false;
        }
        else if (distance < stopRadius)
        {
            parked = true;
        }

        if (parked)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, autoBraking * Time.fixedDeltaTime);
            rb.velocity = heading * currentSpeed;
            return;
        }

        Vector2 desired = toCursor / distance;

        // Target speed = cap * distance falloff * alignment falloff.
        float distanceFactor = targetSpeedByDistance.Evaluate(Mathf.Clamp01(distance / arrivalRange));
        float alignment = Vector2.Dot(heading, desired);
        float alignmentFactor = speedByAlignment.Evaluate(alignment);
        float targetSpeed = maxSpeed * distanceFactor * alignmentFactor;

        // Asymmetric ramp: soft acceleration, firm braking.
        float rate = targetSpeed > currentSpeed ? acceleration : autoBraking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // Turn rate falls with speed -> large radius at top speed; min turning radius ~ currentSpeed / (turnRate deg-to-rad).
        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);
        float turnRate = turnRateBySpeed.Evaluate(speedT);
        float maxRadians = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        heading = Vector3.RotateTowards(heading, desired, maxRadians, 0f);

        rb.velocity = heading * currentSpeed;
        rb.MoveRotation(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + spriteHeadOffsetDeg);
    }
}
