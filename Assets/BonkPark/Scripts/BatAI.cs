using UnityEngine;

// 蝙蝠追逐 AI。读取 Lumi 身上的 ReactionBuffer 拿延迟位置作为目标点，朝目标飞 ——
// 朝向匹配时全速、偏离时主动降速给小半径转弯让路，转向角速度也随速度变化（低速
// 灵活、高速稳定）。两条曲线 + 不对称加减速让蝙蝠在玩家急转时自然甩尾，不会高
// 速画大圈。接触玩家触发 PlayerDeath，靠物理碰撞被 ParkBoundary 挡在场内。
[RequireComponent(typeof(Rigidbody2D))]
public class BatAI : MonoBehaviour
{
    [Header("Target")]

    [Tooltip("Lumi 身上的 ReactionBuffer 引用。蝙蝠查询此 buffer 拿延迟位置。多个敌人可以共享同一个 buffer。")]
    [SerializeField] ReactionBuffer target;

    [Tooltip("反应延迟（秒）。蝙蝠追的是 delay 秒前的 Lumi 位置 —— 数值越大越好骗。设计区间 0.25 ~ 0.5。")]
    [Range(0.05f, 0.8f)]
    [SerializeField] float reactionDelay = 0.4f;

    [Header("Movement")]

    [Tooltip("蝙蝠最大速度（m/s）。比 Lumi maxSpeed 略高使追击有压迫感，但被转向曲线限制后实际很难贴脸。")]
    [SerializeField] float maxSpeed = 13f;

    [Tooltip("加速度（m/s²）。目标速度高于当前速度时使用，决定蝙蝠起步多快。")]
    [SerializeField] float acceleration = 12f;

    [Tooltip("自动刹车减速度（m/s²）。目标速度低于当前速度时使用 —— 朝向偏离时蝙蝠主动减速、转完再加回去。需要明显大于 acceleration。")]
    [SerializeField] float autoBraking = 25f;

    [Tooltip("朝向匹配度 → 目标速度因子。X: dot(heading, desired) (-1~1)，Y: 速度倍率 (0~1)。同向全速、侧向降速、反向几乎归零（强调'转身需要时间'，蝙蝠的弱点）。")]
    [SerializeField] AnimationCurve speedByAlignment;

    [Tooltip("速度 → 最大转向角速度。X: currentSpeed / maxSpeed (0~1)，Y: 角速度 deg/s。低速给高灵活度，高速限制角速度 = 大半径。满速 ~120°/s 对齐 GameConceptDocument 基线。")]
    [SerializeField] AnimationCurve turnRateBySpeed;

    [Header("Sprite")]

    [Tooltip("是否按 heading.x 符号翻转 sprite（蝙蝠图本身正面对镜头，不旋转 sprite，只水平翻转最自然）。")]
    [SerializeField] bool flipSpriteByHeading = true;

    [Header("Init")]

    [Tooltip("蝙蝠启动朝向，模长非零即可（自动归一化）。")]
    [SerializeField] Vector2 initialHeading = Vector2.right;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 heading;
    float currentSpeed;

    // Inspector 右键 Reset 调用。给两条没有现成 helper 的曲线写默认关键帧。
    void Reset()
    {
        speedByAlignment = new AnimationCurve(
            new Keyframe(-1f, 0.1f),    // 反向：几乎停下，转身耗时
            new Keyframe(0f, 0.35f),    // 侧向：低速便于转弯
            new Keyframe(1f, 1f)        // 同向：全速
        );
        turnRateBySpeed = new AnimationCurve(
            new Keyframe(0f, 360f),     // 低速：原地灵活
            new Keyframe(0.5f, 200f),
            new Keyframe(1f, 120f)      // 满速：对齐 GameConceptDocument 基线
        );
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.right;
        currentSpeed = 0f;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        Vector2 snapshot = target.GetSnapshot(reactionDelay);
        Vector2 toTarget = snapshot - rb.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector2 desired = toTarget.normalized;

        // 目标速度按朝向匹配度衰减：同向全速、侧向降速给转弯让路、反向近乎归零
        float alignment = Vector2.Dot(heading, desired);
        float targetSpeed = maxSpeed * speedByAlignment.Evaluate(alignment);

        // 升档用柔加速、降档用强刹车，配合 alignment 衰减让蝙蝠转向时主动甩尾
        float rate = targetSpeed > currentSpeed ? acceleration : autoBraking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // 转向角速度由当前速度查曲线决定：低速 = 小半径快速调头、高速 = 大半径稳定
        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);
        float turnRate = turnRateBySpeed.Evaluate(speedT);
        float maxRadians = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        heading = Vector3.RotateTowards(heading, desired, maxRadians, 0f);

        rb.velocity = heading * currentSpeed;

        if (flipSpriteByHeading && sr != null && Mathf.Abs(heading.x) > 0.01f)
            sr.flipX = heading.x < 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var death = collision.gameObject.GetComponent<PlayerDeath>();
        if (death != null) death.Die();
    }
}
