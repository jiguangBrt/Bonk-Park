using UnityEngine;

// A bush both Lumi and the bat can move through, but it drags on whoever's inside.
// Lumi can dash through to skip the slow, since the dash sets her speed directly.
[RequireComponent(typeof(Collider2D))]
public class BushZone : MonoBehaviour
{
    [Tooltip("Speed fraction for anything inside the bush. 1 = no slow, 0.5 = half speed.")]
    [Range(0f, 1f)]
    [SerializeField] float slowMultiplier = 0.5f;

    void OnTriggerEnter2D(Collider2D other)
    {
        var body = other.attachedRigidbody;
        if (body == null) return;
        var player = body.GetComponent<PlayerController>();
        if (player != null) { player.EnterBush(slowMultiplier); return; }
        var bat = body.GetComponent<BatAI>();
        if (bat != null) bat.EnterBush(slowMultiplier);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var body = other.attachedRigidbody;
        if (body == null) return;
        var player = body.GetComponent<PlayerController>();
        if (player != null) { player.ExitBush(); return; }
        var bat = body.GetComponent<BatAI>();
        if (bat != null) bat.ExitBush();
    }
}
