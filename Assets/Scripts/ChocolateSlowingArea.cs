using UnityEngine;

public class ChocolateSlowingArea : MonoBehaviour
{
    [Header("减速效果设置")]
    public float slowFactor = 0.5f;  // 减速系数（0-1，越小减速越多）
    public bool resetSpeedOnExit = true; // 离开时是否恢复速度

    [Header("视觉效果")]
    public ParticleSystem meltingParticles; // 融化粒子效果
    public Material meltedMaterial; // 融化后的材质

    [Header("UI提示")]
    public Color slowMessageColor = new Color(1f, 1f, 1f); // 提示文字颜色

    private float originalRunSpeed;
    private float originalSideSpeed;
    private PlayerController player;
    private bool isPlayerInside = false;
    private Material originalMaterial;
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }

        // 自动添加触发器
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                isPlayerInside = true;

                // 保存原始速度
                originalRunSpeed = player.runSpeed;
                originalSideSpeed = player.sideMoveSpeed;

                // 应用减速
                player.runSpeed *= slowFactor;
                player.sideMoveSpeed *= slowFactor;

                // 触发视觉效果
                StartMeltingEffect();

                // 显示减速提示
                ShowSlowMessage();

                Debug.Log($"进入区域，速度减至 {slowFactor * 100}%");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && player != null && resetSpeedOnExit)
        {
            isPlayerInside = false;

            // 恢复原始速度
            player.runSpeed = originalRunSpeed;
            player.sideMoveSpeed = originalSideSpeed;

            // 停止视觉效果
            StopMeltingEffect();

            // 显示恢复速度提示
            ShowRecoveryMessage();

            Debug.Log("离开区域，速度恢复");
        }
    }

    void StartMeltingEffect()
    {
        // 播放粒子效果
        if (meltingParticles != null)
        {
            meltingParticles.Play();
        }

        // 切换材质
        if (meshRenderer != null && meltedMaterial != null)
        {
            meshRenderer.material = meltedMaterial;
        }
    }

    void StopMeltingEffect()
    {
        // 停止粒子效果
        if (meltingParticles != null)
        {
            meltingParticles.Stop();
        }

        // 恢复原始材质
        if (meshRenderer != null && originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
    }

    // 显示减速提示（模仿HealPotion的实现方式）
    void ShowSlowMessage()
    {
        // 计算减速百分比
        float slowPercentage = (1 - slowFactor) * 100;
        string message = $"减速区域！速度-{slowPercentage:F0}%";

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEffectTip(message, slowMessageColor);
        }
        else
        {
            Debug.LogWarning("UIManager.Instance 为空，无法显示减速提示");
        }
    }

    // 显示恢复速度提示
    void ShowRecoveryMessage()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEffectTip("离开减速区域，速度恢复", Color.green);
        }
    }

    void OnDestroy()
    {
        // 确保玩家离开时恢复速度
        if (isPlayerInside && player != null && resetSpeedOnExit)
        {
            player.runSpeed = originalRunSpeed;
            player.sideMoveSpeed = originalSideSpeed;

            // 如果UI管理器存在，显示恢复提示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEffectTip("区域消失，速度恢复", Color.green);
            }
        }
    }
}