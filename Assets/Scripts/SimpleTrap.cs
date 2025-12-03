using UnityEngine;

public class SimpleTrap : MonoBehaviour
{
    [Header("陷阱设置")]
    public bool isActive = true;
    public float damageDelay = 0f;
    public float triggerCooldown = 2f; // 触发冷却时间

    [Header("视觉效果")]
    public GameObject[] spikeMeshes;
    public float spikeHeight = 0.3f;
    public float spikeSpeed = 5f;

    [Header("碰撞检测")]
    public bool showDebugInfo = true;

    private bool isTriggered = false;
    private Vector3[] originalSpikePositions;
    private Vector3[] targetSpikePositions;
    private float spikeProgress = 0f;
    private float lastTriggerTime = -10f;
    private BoxCollider triggerCollider;

    void Start()
    {
        // 设置标签
        gameObject.tag = "Trap";

        // 初始化刺的位置
        if (spikeMeshes != null && spikeMeshes.Length > 0)
        {
            originalSpikePositions = new Vector3[spikeMeshes.Length];
            targetSpikePositions = new Vector3[spikeMeshes.Length];

            for (int i = 0; i < spikeMeshes.Length; i++)
            {
                if (spikeMeshes[i] != null)
                {
                    originalSpikePositions[i] = spikeMeshes[i].transform.localPosition;
                    targetSpikePositions[i] = originalSpikePositions[i] + new Vector3(0, spikeHeight, 0);
                    spikeMeshes[i].transform.localPosition = originalSpikePositions[i];
                }
            }
        }

        // 确保有合适的触发器碰撞器
        SetupTriggerCollider();
    }

    void SetupTriggerCollider()
    {
        // 先检查是否已有碰撞器
        Collider existingCollider = GetComponent<Collider>();

        if (existingCollider == null)
        {
            // 添加新的Box Collider作为触发器
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;

            // 设置合适的大小
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                triggerCollider.size = renderer.bounds.size;
                triggerCollider.center = renderer.bounds.center - transform.position;
            }
            else
            {
                // 默认大小
                triggerCollider.size = new Vector3(2f, 2f, 2f);
                triggerCollider.center = new Vector3(0, 0, 0);
            }

            if (showDebugInfo)
            {
                Debug.Log($"为陷阱 {gameObject.name} 创建触发器碰撞器，大小: {triggerCollider.size}");
            }
        }
        else
        {
            // 确保现有的碰撞器是触发器
            existingCollider.isTrigger = true;
            if (showDebugInfo)
            {
                Debug.Log($"使用现有碰撞器作为触发器: {existingCollider.name}");
            }
        }
    }

    void Update()
    {
        // 处理刺的动画
        if (isTriggered && spikeMeshes != null)
        {
            spikeProgress = Mathf.Min(spikeProgress + Time.deltaTime * spikeSpeed, 1f);

            for (int i = 0; i < spikeMeshes.Length; i++)
            {
                if (spikeMeshes[i] != null)
                {
                    spikeMeshes[i].transform.localPosition = Vector3.Lerp(
                        originalSpikePositions[i],
                        targetSpikePositions[i],
                        spikeProgress
                    );
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // 冷却时间检查
        if (Time.time - lastTriggerTime < triggerCooldown) return;

        if (other.CompareTag("Player"))
        {
            if (showDebugInfo)
            {
                Debug.Log($"陷阱 {gameObject.name} 检测到玩家进入触发器");
            }

            lastTriggerTime = Time.time;
            TriggerTrap();
        }
    }

    void TriggerTrap()
    {
        if (isTriggered) return;

        isTriggered = true;

        if (showDebugInfo)
        {
            Debug.Log($"陷阱 {gameObject.name} 被触发!");
        }

        // 弹出刺
        spikeProgress = 0f;

        // 立即对玩家造成伤害
        DamagePlayer();
    }

    void DamagePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ObstacleCollision obstacleCollision = player.GetComponent<ObstacleCollision>();
            if (obstacleCollision != null)
            {
                // 直接调用方法，不使用SendMessage
                if (showDebugInfo)
                {
                    Debug.Log($"调用玩家 {player.name} 的 TriggerGameOver 方法");
                }

                // 使用Invoke调用，确保正确执行
                obstacleCollision.Invoke("TriggerGameOver", 0f);
            }
            else
            {
                Debug.LogError($"玩家对象 {player.name} 上没有找到 ObstacleCollision 组件！");
            }
        }
        else
        {
            Debug.LogError("找不到玩家对象！");
        }
    }

    // 重置陷阱
    public void ResetTrap()
    {
        isTriggered = false;
        spikeProgress = 0f;

        if (spikeMeshes != null)
        {
            for (int i = 0; i < spikeMeshes.Length; i++)
            {
                if (spikeMeshes[i] != null)
                {
                    spikeMeshes[i].transform.localPosition = originalSpikePositions[i];
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // 绘制触发器范围（黄色）
        Gizmos.color = Color.yellow;
        Collider collider = GetComponent<Collider>();
        if (collider != null && collider.enabled)
        {
            Gizmos.DrawWireCube(transform.position + collider.bounds.center, collider.bounds.size);
        }

        // 如果已触发，绘制红色边框
        if (isTriggered)
        {
            Gizmos.color = Color.red;
            if (collider != null)
            {
                Gizmos.DrawWireCube(transform.position + collider.bounds.center, collider.bounds.size * 1.1f);
            }
        }
    }
}