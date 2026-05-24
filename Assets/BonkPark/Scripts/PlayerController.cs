using UnityEngine;

// 鼠标驱动的萤火虫自动巡航控制器。鼠标位置即目标点：远处全速冲、近处自动减速、
// 越过鼠标视速度决定停下还是回头。左键刹车（BrakeSystem）是另一套系统，本控制器
// 不负责急停急转，所有参数留出量级差给玩家的主动操作。
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]

    [Tooltip("Lumi 速度上限（m/s）。所有 alignment / distance 因子最终都乘以这个值。")]
    [SerializeField] float maxSpeed = 12f;

    [Tooltip("鼠标距 Lumi 多远开始进入减速区（m）。距离归一化到这个范围后送入 targetSpeedByDistance 曲线，超出此距离按曲线 1.0 端全速冲。")]
    [SerializeField] float arrivalRange = 2.5f;

    [Tooltip("加速度（m/s²）。仅在目标速度高于当前速度时使用，决定 Lumi 起步多快。")]
    [SerializeField] float acceleration = 15f;

    [Tooltip("自动刹车减速度（m/s²）。目标速度低于当前速度时使用。比 acceleration 大（真实生物刹车快于加速），但要明显小于未来左键刹车（60+），不抢主动刹车的戏。")]
    [SerializeField] float autoBraking = 25f;

    [Tooltip("进入停车状态的门槛（m）。鼠标距 Lumi 小于此值时锁定 parked = true：停止接受鼠标转向、按 autoBraking 减速到 0。")]
    [SerializeField] float stopRadius = 0.4f;

    [Tooltip("解除停车状态的门槛（m），必须 > stopRadius 形成迟滞。Lumi 完全停下后，若距鼠标仍超过此值才重新接管控制。stopRadius 与 resumeRadius 之间是允许的'轻微越过'区，越过越多 → 越可能触发回头。")]
    [SerializeField] float resumeRadius = 1.0f;

    [Tooltip("距离 → 目标速度因子。X: distance / arrivalRange (0~1)，Y: 速度倍率 (0~1)。曲线在 0 处 = 0 实现贴脸停下，1 处 = 1 实现远距全速。ease-in-out 形状给减速段一个柔和的过渡。")]
    [SerializeField] AnimationCurve targetSpeedByDistance = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("朝向匹配度 → 目标速度因子。X: dot(heading, desired) (-1~1)，Y: 速度倍率 (0~1)。同向时全速、侧向降速给小半径转弯让位、反向不归零（保留小圆调头），不与左键刹车的'瞬间归零'手感重叠。")]
    [SerializeField] AnimationCurve speedByAlignment;

    [Header("Turning")]

    [Tooltip("速度 → 最大转向角速度。X: currentSpeed / maxSpeed (0~1)，Y: 角速度 deg/s。低速给高灵活度，高速限制角速度 = 大半径稳定。决定满速最小转弯半径 = currentSpeed / (turnRate · π/180)。")]
    [SerializeField] AnimationCurve turnRateBySpeed;

    [Tooltip("Sprite 朝向修正（度）。Sprite 静止时'头'指向哪 = 这里填多少的负值。例：sprite 头朝上画 → 填 -90（heading 朝右时整体旋转 -90° 让头朝右）。换素材记得改。")]
    [SerializeField] float spriteHeadOffsetDeg = -90f;

    [Header("Init")]

    [Tooltip("Lumi 启动朝向，模长非零即可（自动归一化）。决定第一帧 sprite 的朝向。")]
    [SerializeField] Vector2 initialHeading = Vector2.up;

    [Tooltip("Lumi 启动速度（m/s）。一般 0；想让 Lumi 开场就有惯性可调高，会被钳制到 [0, maxSpeed]。")]
    [SerializeField] float initialSpeed = 0f;

    Rigidbody2D rb;
    Vector2 heading;       // 当前朝向，单位向量
    float currentSpeed;    // 当前速度标量；velocity = heading * currentSpeed
    bool parked;           // 停车状态期间锁朝向、只减速、不响应鼠标

    // Inspector 右键 Reset 调用。给两条没有现成 helper 的曲线写默认关键帧。
    void Reset()
    {
        targetSpeedByDistance = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        speedByAlignment = new AnimationCurve(
            new Keyframe(-1f, 0.2f),   // 反向：低速画小圆调头
            new Keyframe(0f, 0.5f),    // 侧向：半速便于转弯
            new Keyframe(1f, 1f)       // 同向：全速
        );
        turnRateBySpeed = new AnimationCurve(
            new Keyframe(0f, 540f),    // 低速：原地灵活
            new Keyframe(0.5f, 240f),
            new Keyframe(1f, 180f)     // 满速：半径 v/ω ≈ 3.8m
        );
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        heading = initialHeading.sqrMagnitude > 0f ? initialHeading.normalized : Vector2.up;
        currentSpeed = Mathf.Clamp(initialSpeed, 0f, maxSpeed);
    }

    // 跑在 FixedUpdate 而非 Update：操作 Rigidbody2D 的 velocity / MoveRotation
    // 必须挂在物理回调里，否则会跟帧率挂钩，产生穿透和抖动。
    void FixedUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toCursor = mouseWorld - rb.position;
        float distance = toCursor.magnitude;

        // 停车状态机。进入用 stopRadius、退出用 resumeRadius + 速度归零双门槛。
        // 速度门是关键：高速越过鼠标时如果光看 distance 解除 parked，会在残余高
        // 速 + 低 alignmentFactor 下进入半死不活的转弯，最终被小速度卡在 idle。
        if (parked)
        {
            if (currentSpeed < 0.01f && distance > resumeRadius) parked = false;
        }
        else if (distance < stopRadius)
        {
            parked = true;
        }

        // 停车分支：朝向锁死，按 autoBraking 减速到 0，允许靠惯性越过鼠标。
        if (parked)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, autoBraking * Time.fixedDeltaTime);
            rb.velocity = heading * currentSpeed;
            return;
        }

        Vector2 desired = toCursor / distance;

        // 目标速度 = 上限 × 距离衰减 × 朝向衰减。
        // 距离衰减处理"远距冲刺、近距缓停"；朝向衰减处理"侧向自动降速给转弯让路"。
        float distanceFactor = targetSpeedByDistance.Evaluate(Mathf.Clamp01(distance / arrivalRange));
        float alignment = Vector2.Dot(heading, desired);
        float alignmentFactor = speedByAlignment.Evaluate(alignment);
        float targetSpeed = maxSpeed * distanceFactor * alignmentFactor;

        // 升档用柔加速，降档用强刹车，模拟真实运动的不对称性。
        float rate = targetSpeed > currentSpeed ? acceleration : autoBraking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // 转向角速度由当前速度查曲线决定，高速 = 大半径。180° 反向时 RotateTowards
        // 会选默认旋转轴，但此时 alignmentFactor ≈ 0.2 已把速度压低，掉头自然平滑。
        float speedT = Mathf.Clamp01(currentSpeed / maxSpeed);
        float turnRate = turnRateBySpeed.Evaluate(speedT);
        float maxRadians = turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        heading = Vector3.RotateTowards(heading, desired, maxRadians, 0f);

        rb.velocity = heading * currentSpeed;
        rb.MoveRotation(Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + spriteHeadOffsetDeg);
    }
}
