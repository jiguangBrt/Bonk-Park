using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles Lumi's death: freezes for a brief feedback window, then reloads the current scene. Die() is idempotent.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    [Tooltip("Freeze duration before reload.")]
    [SerializeField] float freezeDuration = 0.3f;

    Rigidbody2D rb;
    PlayerController controller;
    bool dying;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
    }

    public void Die()
    {
        if (dying) return;
        dying = true;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        if (controller != null) controller.enabled = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;
        yield return new WaitForSeconds(freezeDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
