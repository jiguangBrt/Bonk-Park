using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float maxSpeed = 12f;
    [SerializeField] float accel = 15f;
    [SerializeField] float turnRateAtLowSpeed = 360f;
    [SerializeField] float turnRateAtHighSpeed = 90f;
    [SerializeField] float spriteHeadOffsetDeg = -90f;
    [SerializeField] float mouseDeadzone = 0.1f;
    [SerializeField] Vector2 initialHeading = Vector2.up;
    [SerializeField] float initialSpeed = 0f;

    Rigidbody2D rb;
    Vector2 heading;
    float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.up;
        currentSpeed = Mathf.Clamp(initialSpeed, 0f, maxSpeed);
    }

    void FixedUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toCursor = mouseWorld - rb.position;

        if (toCursor.magnitude > mouseDeadzone)
        {
            Vector2 desired = toCursor.normalized;
            float t = Mathf.Clamp01(currentSpeed / maxSpeed);
            float turnRate = Mathf.Lerp(turnRateAtLowSpeed, turnRateAtHighSpeed, t);
            float maxRadians = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
            heading = Vector3.RotateTowards(heading, desired, maxRadians, 0f);
        }

        currentSpeed = Mathf.Min(currentSpeed + accel * Time.fixedDeltaTime, maxSpeed);

        rb.velocity = heading * currentSpeed;
        rb.MoveRotation(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + spriteHeadOffsetDeg);
    }
}
