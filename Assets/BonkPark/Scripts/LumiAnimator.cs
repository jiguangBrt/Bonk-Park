using UnityEngine;

// Drives Lumi's idle/move animation from its actual speed. Parking and braking both read as idle because velocity drops to near zero.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class LumiAnimator : MonoBehaviour
{
    [Tooltip("Speed to enter the move state, m/s.")]
    [SerializeField] float moveEnterSpeed = 0.5f;

    [Tooltip("Speed to fall back to idle, m/s.")]
    [SerializeField] float moveExitSpeed = 0.2f;

    Rigidbody2D rb;
    Animator animator;
    bool moving;

    static readonly int MovingId = Animator.StringToHash("Moving");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Hysteresis band between the two thresholds keeps the firefly from flickering idle<->move while it crawls through park-mode deceleration.
        float speed = rb.velocity.magnitude;
        bool wantMove = moving ? speed > moveExitSpeed : speed > moveEnterSpeed;
        if (wantMove != moving)
        {
            moving = wantMove;
            animator.SetBool(MovingId, moving);
        }
    }
}
