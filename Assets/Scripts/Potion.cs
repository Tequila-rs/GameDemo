using UnityEngine;
using System.Collections.Generic;

public class Potion : MonoBehaviour
{
    [Header("药水基础设置")]
    public float duration = 10f; // 速度药水持续时间
    public int healAmount = 20; // 治疗量
    public int lookBackCharges = 1; // 增加回头次数
    public float speedMultiplier = 1.5f; // 速度倍率

    // 可视化调试用的碰撞范围（避免强制类型转换）
    [Header("调试设置")]
    public float gizmoRadius = 0.5f;

    [Header("重置设置")]
    public bool respawnOnRestart = true; // 游戏重新开始时是否重新生成
    public float respawnDelay = 0.5f; // 重新生成的延迟时间

    // 静态列表保存所有药水实例，用于重置
    private static List<Potion> allPotions = new List<Potion>();
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isActive = true;
    private Collider potionCollider;
    private Renderer potionRenderer;

    // 确保碰撞体和刚体正确设置
    private void Awake()
    {
        // 添加到全局列表
        if (!allPotions.Contains(this))
        {
            allPotions.Add(this);
        }

        // 保存原始位置和旋转
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // 获取组件
        potionCollider = GetComponent<Collider>();
        potionRenderer = GetComponent<Renderer>();

        // 自动添加碰撞体（如果没有）
        if (potionCollider == null)
        {
            // 默认添加球形碰撞体，避免类型转换问题
            potionCollider = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)potionCollider).radius = gizmoRadius;
        }
        potionCollider.isTrigger = true; // 强制设为触发器

        // 添加刚体（防止药水掉落，且保证碰撞检测生效）
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true; // 固定位置，不被物理影响
        rb.useGravity = false;
    }

    // 修复方法名拼写错误（之前是ApplyEffectEffect）
    protected virtual void ApplyEffect(PlayerController player)
    {
        // 由子类实现具体效果
    }

    // 确保碰撞检测生效
    private void OnTriggerEnter(Collider other)
    {
        // 如果药水不活跃，不处理碰撞
        if (!isActive) return;

        Debug.Log($"药水检测到碰撞: {other.gameObject.name}, 标签: {other.tag}");

        // 检查是否碰撞到玩家
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ApplyEffect(player);
                Debug.Log("成功拾取药水，触发效果");

                // 拾取后隐藏药水，而不是销毁
                HidePotion();
            }
            else
            {
                Debug.LogError("碰撞到的对象是Player标签，但没有PlayerController组件！");
            }
        }
    }

    // 隐藏药水而不是销毁
    private void HidePotion()
    {
        isActive = false;

        if (potionCollider != null)
            potionCollider.enabled = false;

        if (potionRenderer != null)
            potionRenderer.enabled = false;

        // 如果有子物体的渲染器也隐藏
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = false;
        }
    }

    // 重新生成药水
    public void RespawnPotion()
    {
        if (!respawnOnRestart) return;

        isActive = true;

        // 重置位置和旋转
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // 启用碰撞体和渲染器
        if (potionCollider != null)
            potionCollider.enabled = true;

        if (potionRenderer != null)
            potionRenderer.enabled = true;

        // 启用子物体的渲染器
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = true;
        }

        Debug.Log($"药水重新生成: {gameObject.name}");
    }

    // 修复OnDrawGizmos的类型转换错误
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // 安全获取碰撞体半径，避免强制转换
        Collider collider = GetComponent<Collider>();
        float radius = gizmoRadius; // 默认半径

        // 只在确认是SphereCollider时才获取其半径
        if (collider is SphereCollider sphereCollider)
        {
            radius = sphereCollider.radius;
        }

        // 绘制碰撞范围（兼容所有碰撞体类型）
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    // 静态方法：重置所有药水
    public static void ResetAllPotions()
    {
        Debug.Log($"开始重置所有药水，共{allPotions.Count}个");

        foreach (Potion potion in allPotions)
        {
            if (potion != null && potion.respawnOnRestart)
            {
                potion.RespawnPotion();
            }
        }
    }

    // 从列表中移除
    private void OnDestroy()
    {
        if (allPotions.Contains(this))
        {
            allPotions.Remove(this);
        }
    }
}