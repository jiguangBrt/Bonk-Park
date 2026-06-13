using UnityEngine;

// Marker for obstacles that bonk pursuers on contact (rocks, trees, lampposts).
// Stun length is per-obstacle so heavier props can hit harder later.
public class Bonkable : MonoBehaviour
{
    [Tooltip("Stun duration on contact, seconds.")]
    [SerializeField] float stunDuration = 0.8f;

    public float StunDuration => stunDuration;

    // Called when a pursuer bonks this obstacle, so props can react (e.g. the lamp short-circuits).
    public virtual void OnBonk() { }
}
