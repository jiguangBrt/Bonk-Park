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
    [SerializeField] float spriteHeadOffsetDeg = -180f;

    [Header("Low Light")]

    [Tooltip("Slowest speed fraction at empty light. Never zero so Lumi can still crawl clear.")]
    [Range(0f, 1f)]
    [SerializeField] float lowLightSpeedFloor = 0.25f;

    [Tooltip("Steepness of the low-light slowdown. 2 = quadratic.")]
    [SerializeField] float lowLightFalloff = 2f;

    [Header("Dash")]

    [Tooltip("Dash travel distance, m.")]
    [SerializeField] float dashDistance = 4f;

    [Tooltip("Dash travel time, s. Lower is snappier; very low reads as a blink.")]
    [SerializeField] float dashDuration = 0.12f;

    [Tooltip("Speed Lumi keeps the moment the dash ends, m/s.")]
    [SerializeField] float dashEndSpeed = 10f;

    [Tooltip("Light spent per dash.")]
    [SerializeField] float dashCost = 20f;

    [Tooltip("Dash cooldown, s.")]
    [SerializeField] float dashCooldown = 0.8f;

    [Header("Init")]

    [Tooltip("Initial heading.")]
    [SerializeField] Vector2 initialHeading = Vector2.up;

    [Tooltip("Initial speed, m/s.")]
    [SerializeField] float initialSpeed = 0f;

    Rigidbody2D rb;
    LumiEnergy energy;
    Vector2 heading;
    float currentSpeed;
    bool parked;
    bool dashQueued;
    float dashCooldownTimer;
    bool dashing;
    float dashTimer;
    Vector2 dashDir;

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
        energy = GetComponent<LumiEnergy>();
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.up;
        currentSpeed = Mathf.Clamp(initialSpeed, 0f, maxSpeed);
    }

    // Left click is a discrete press; read it in Update so a click landing between physics steps isn't dropped.
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) dashQueued = true;
    }

    void FixedUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector2 toCursor = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) - rb.position;
        float distance = toCursor.magnitude;

        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.fixedDeltaTime);

        if (dashQueued)
        {
            dashQueued = false;
            if (!dashing && dashCooldownTimer <= 0f && (energy == null || !energy.IsLowLight) && distance > 0.0001f)
                StartDash(toCursor / distance);
        }

        if (dashing)
        {
            TickDash();
            return;
        }

        UpdateParkedState(distance);
        if (parked)
        {
            DecelerateParked();
            return;
        }

        Vector2 desired = toCursor / distance;
        float targetSpeed = ComputeTargetSpeed(distance, desired);
        UpdateSpeed(targetSpeed);
        UpdateHeading(desired);
        WriteVelocityAndRotation();
    }

    // Distance hysteresis plus a speed gate so high-speed overshoot doesn't leave us limping inside stopRadius.
    void UpdateParkedState(float distance)
    {
        if (parked)
        {
            if (currentSpeed < 0.01f && distance > resumeRadius) parked = false;
        }
        else if (distance < stopRadius)
        {
            parked = true;
        }
    }

    void DecelerateParked()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, autoBraking * Time.fixedDeltaTime);
        rb.velocity = heading * currentSpeed;
    }

    // Fixed-distance dash, always toward the cursor: snaps heading to the cursor and covers dashDistance over
    // dashDuration, so distance is a direct dial rather than a side effect of speed. Velocity-driven, so walls still stop it.
    void StartDash(Vector2 dir)
    {
        dashDir = dir;
        heading = dir;
        dashing = true;
        dashTimer = dashDuration;
        parked = false;

        if (energy != null)
        {
            energy.Consume(dashCost);
            energy.Flare();
        }
        dashCooldownTimer = dashCooldown;
    }

    void TickDash()
    {
        float speed = dashDistance / Mathf.Max(0.0001f, dashDuration);
        rb.velocity = dashDir * speed;
        rb.MoveRotation(Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg + spriteHeadOffsetDeg);

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0f)
        {
            dashing = false;
            currentSpeed = dashEndSpeed;
        }
    }

    // Target speed = cap * distance falloff * alignment falloff.
    float ComputeTargetSpeed(float distance, Vector2 desired)
    {
        float distanceFactor = targetSpeedByDistance.Evaluate(Mathf.Clamp01(distance / arrivalRange));
        float alignment = Vector2.Dot(heading, desired);
        float alignmentFactor = speedByAlignment.Evaluate(alignment);
        return maxSpeed * distanceFactor * alignmentFactor * LowLightSpeedMultiplier();
    }

    // Full speed above the low-light threshold; below it, speed falls along (light/threshold)^falloff
    // toward a floor so a dimming Lumi crawls but never freezes.
    float LowLightSpeedMultiplier()
    {
        if (energy == null || !energy.IsLowLight) return 1f;
        float t = Mathf.Clamp01(energy.Normalized / energy.LowLightThreshold);
        return Mathf.Lerp(lowLightSpeedFloor, 1f, Mathf.Pow(t, lowLightFalloff));
    }

    // Asymmetric ramp: soft acceleration, firm braking.
    void UpdateSpeed(float targetSpeed)
    {
        float rate = targetSpeed > currentSpeed ? acceleration : autoBraking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);
    }

    // Turn rate falls with speed -> large radius at top speed; min turning radius ~ currentSpeed / (turnRate deg-to-rad).
    void UpdateHeading(Vector2 desired)
    {
        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);
        float turnRate = turnRateBySpeed.Evaluate(speedT);
        float maxRadians = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        heading = Vector3.RotateTowards(heading, desired, maxRadians, 0f);
    }

    void WriteVelocityAndRotation()
    {
        rb.velocity = heading * currentSpeed;
        rb.MoveRotation(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + spriteHeadOffsetDeg);
    }
}
