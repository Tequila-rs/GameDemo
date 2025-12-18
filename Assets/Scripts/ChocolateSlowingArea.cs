using UnityEngine;

public class ChocolateSlowingArea : MonoBehaviour
{
    [Header("减速效果设置")]
    public float slowFactor = 0.5f;  // 减速系数（0-1，越小减速越多）
    public bool resetSpeedOnExit = true; // 离开时是否恢复速度

    [Header("视觉效果")]
    public ParticleSystem meltingParticles; // 融化粒子效果
    public Material meltedMaterial; // 融化后的材质

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

                Debug.Log($"进入巧克力区域，速度减至 {slowFactor * 100}%");
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

            Debug.Log("离开巧克力区域，速度恢复");
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

    void OnDestroy()
    {
        // 确保玩家离开时恢复速度
        if (isPlayerInside && player != null && resetSpeedOnExit)
        {
            player.runSpeed = originalRunSpeed;
            player.sideMoveSpeed = originalSideSpeed;
        }
    }
}