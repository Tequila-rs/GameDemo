using UnityEngine;

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

    // 确保碰撞体和刚体正确设置
    private void Awake()
    {
        // 自动添加碰撞体（如果没有）
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            // 默认添加球形碰撞体，避免类型转换问题
            collider = gameObject.AddComponent<SphereCollider>();
            ((SphereCollider)collider).radius = gizmoRadius;
        }
        collider.isTrigger = true; // 强制设为触发器

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
        Debug.Log($"药水检测到碰撞: {other.gameObject.name}, 标签: {other.tag}");

        // 检查是否碰撞到玩家
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ApplyEffect(player);
                Debug.Log("成功拾取药水，触发效果");
                Destroy(gameObject); // 拾取后销毁药水
            }
            else
            {
                Debug.LogError("碰撞到的对象是Player标签，但没有PlayerController组件！");
            }
        }
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
}