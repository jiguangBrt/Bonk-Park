using System.Collections;
using TMPro;
using UnityEngine;

// First-run, hands-on tutorial slotted between the opening and the chase. Lumi arrives dark and powerless: two
// motes drift in beside him and feed his glow, so the player reads "motes are energy, energy is light" before they
// touch the controls. Only then does the mouse take over, the dash is taught, and — once that lands — the chase
// begins. The bat is held back and the passive drain paused throughout, so there is no death pressure.
public class OnboardingSequence : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] LumiEnergy lumi;
    [SerializeField] PlayerController player;
    [SerializeField] GlowMoteSpawner spawner;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text prompt;

    [Header("Lines")]
    [TextArea] [SerializeField] string energyLine = "Energy is your light. Gather motes to keep it burning.";
    [TextArea] [SerializeField] string moveLine = "Move your mouse — Lumi follows your cursor.";
    [TextArea] [SerializeField] string dashLine = "Left-click to dash for a quick escape.";
    [TextArea] [SerializeField] string chaseLine = "Now run — don't let the bat catch you!";

    [Header("Pacing, seconds")]

    [Tooltip("Fade-in time of each prompt; larger is gentler/slower.")]
    [SerializeField] float fadeIn = 1.1f;

    [Tooltip("Fade-out time of each prompt.")]
    [SerializeField] float fadeOut = 0.7f;

    [Tooltip("Beat held after the motes light Lumi, before the mouse takes over.")]
    [SerializeField] float lightHold = 2f;

    [Tooltip("How long the movement hint lingers once control is handed over.")]
    [SerializeField] float moveHold = 3.5f;

    [Tooltip("How long the final 'now run' line lingers before the bat wakes.")]
    [SerializeField] float chaseHold = 3f;

    [Tooltip("How far the two feeder motes sit either side of Lumi, m. Keep inside the absorb range.")]
    [SerializeField] float moteOffset = 1.8f;

    [Header("Prompt breath")]
    [SerializeField] float breathSpeed = 2.2f;
    [SerializeField] float breathAmount = 0.18f;

    public IEnumerator Run()
    {
        if (group != null) group.alpha = 0f;
        if (lumi != null) lumi.DrainPaused = true;
        if (player != null) player.enabled = false;

        // Two motes drift into the dark firefly and his glow comes up with the energy.
        yield return Show(energyLine);
        GlowMote left = SpawnMoteBeside(Vector2.left);
        GlowMote right = SpawnMoteBeside(Vector2.right);
        while (left != null || right != null) yield return Breathe();
        if (lumi != null) lumi.Flare();
        yield return Hold(lightHold);
        yield return Hide();

        // The mouse takes over.
        if (player != null) player.enabled = true;
        yield return Show(moveLine);
        yield return Hold(moveHold);
        yield return Hide();

        // Dash — wait for a real left-click dash, caught on its rising edge.
        yield return Show(dashLine);
        bool was = player != null && player.Dashing;
        while (true)
        {
            bool now = player != null && player.Dashing;
            if (now && !was) break;
            was = now;
            yield return Breathe();
        }
        yield return Hide();

        // Fully charged for the chase, with a last word before the bat wakes.
        if (lumi != null) { lumi.SetEnergy(lumi.MaxEnergy); lumi.Flare(); }
        yield return Show(chaseLine);
        yield return Hold(chaseHold);
        yield return Hide();

        if (lumi != null) lumi.DrainPaused = false;
    }

    GlowMote SpawnMoteBeside(Vector2 dir)
    {
        if (spawner == null || lumi == null) return null;
        Vector2 pos = (Vector2)lumi.transform.position + dir.normalized * moteOffset;
        return spawner.SpawnAt(pos);
    }

    IEnumerator Show(string text)
    {
        if (prompt != null) { prompt.text = text; prompt.alpha = 1f; }
        yield return Fade(0f, 1f, fadeIn);
    }

    IEnumerator Hide()
    {
        yield return Fade(group != null ? group.alpha : 1f, 0f, fadeOut);
    }

    IEnumerator Hold(float duration)
    {
        float t = 0f;
        while (t < duration) { t += Time.deltaTime; if (prompt != null) prompt.alpha = Breath(); yield return null; }
    }

    IEnumerator Breathe()
    {
        if (prompt != null) prompt.alpha = Breath();
        yield return null;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (group != null) group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        if (group != null) group.alpha = to;
    }

    float Breath()
    {
        return 1f - breathAmount + breathAmount * Mathf.Sin(Time.time * breathSpeed);
    }
}
