using UnityEngine;

// 位置历史环形缓冲。每个 FixedUpdate 采样一次自身 position，供敌人 AI 查询若干秒
// 前的位置快照。BatAI / 未来 OwlAI 共享同一个 buffer，避免每个敌人独立维护历史。
public class ReactionBuffer : MonoBehaviour
{
    [Tooltip("最大可查询延迟（秒）。决定 buffer 容量 = ceil(maxDelay / fixedDeltaTime) + 余量。设大些不影响性能，但查询时 delay 不能超过它。")]
    [SerializeField] float maxDelay = 1.0f;

    Vector2[] positions;
    float[] times;
    int head;       // 下一个写入位置
    int count;      // 已写入样本数；填满后恒等于 capacity

    void Awake()
    {
        int capacity = Mathf.CeilToInt(maxDelay / Time.fixedDeltaTime) + 8;
        positions = new Vector2[capacity];
        times = new float[capacity];
    }

    void FixedUpdate()
    {
        positions[head] = (Vector2)transform.position;
        times[head] = Time.fixedTime;
        head = (head + 1) % positions.Length;
        if (count < positions.Length) count++;
    }

    // 返回 delaySeconds 秒前最近的位置快照。历史不足时返回最早可用样本（开局头几帧
    // 蝙蝠会追当前位置，随着 buffer 填充自然过渡到滞后追击）。
    public Vector2 GetSnapshot(float delaySeconds)
    {
        if (count == 0) return transform.position;
        float target = Time.fixedTime - delaySeconds;

        // 从最新往最旧倒着扫，找第一个时间 <= target 的样本
        for (int i = 1; i <= count; i++)
        {
            int idx = (head - i + positions.Length) % positions.Length;
            if (times[idx] <= target) return positions[idx];
        }

        // 历史不够长，返回最旧样本：未填满时是 index 0，已满时是 head 指向的位置
        int oldest = count < positions.Length ? 0 : head;
        return positions[oldest];
    }
}
