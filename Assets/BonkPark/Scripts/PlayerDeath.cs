using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lumi 死亡处理。冻结 freezeDuration 秒给玩家一个"被抓到"的反馈瞬间，然后重载当前
// 场景。Die() 幂等：dying 标志位锁住后续触发，防止同一帧内多次接触触发多次重载。
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    [Tooltip("死亡冻结时长（秒）。冻结期间禁用 PlayerController、速度归零、刚体设 Static。0.3s 对齐 GameConceptDocument 的死亡反馈时长。")]
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
