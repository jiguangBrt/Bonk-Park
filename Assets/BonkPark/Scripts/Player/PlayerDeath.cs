using System.Collections;
using UnityEngine;

// Handles Lumi's death: freezes Lumi and the predators, pushes the camera in, fades the glow out, then raises the
// death screen. Die() is idempotent. The run does not reload here — the death screen's Play Again does that.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    [Tooltip("Camera push-in and glow fade duration before the death screen, seconds.")]
    [SerializeField] float zoomDuration = 0.9f;

    [Tooltip("Orthographic size the camera tightens to on death.")]
    [SerializeField] float targetSize = 2.5f;

    [SerializeField] LumiEnergy lumi;
    [SerializeField] CameraDeathZoom cameraZoom;
    [SerializeField] DeathScreen deathScreen;
    [SerializeField] CompanionsSaved companions;

    Rigidbody2D rb;
    PlayerController controller;
    bool dying;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        if (lumi == null) lumi = GetComponent<LumiEnergy>();
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

        foreach (var bat in FindObjectsOfType<BatAI>())
        {
            bat.enabled = false;
            var batRb = bat.GetComponent<Rigidbody2D>();
            if (batRb != null) { batRb.velocity = Vector2.zero; batRb.angularVelocity = 0f; }
        }

        if (lumi != null) { lumi.DrainPaused = true; lumi.Extinguish(zoomDuration); }
        if (cameraZoom != null) cameraZoom.PlayZoom(transform, zoomDuration, targetSize);
        if (companions != null) companions.StopCounting();

        yield return new WaitForSeconds(zoomDuration);

        if (deathScreen != null)
            deathScreen.Show(companions != null ? companions.Saved : 0, companions != null ? companions.SurvivalTime : 0f);
    }
}
