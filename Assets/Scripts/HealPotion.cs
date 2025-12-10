using UnityEngine;

public class HealPotion : Potion
{
    [Header("治疗药水特效")]
    public ParticleSystem healEffect;

    // 内置简单的生命值逻辑（如果没有PlayerHealth组件也能生效）
    private void HealPlayer(PlayerController player)
    {
        // 尝试获取PlayerHealth组件
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(healAmount);
        }
        else
        {
            // 如果没有PlayerHealth，直接在PlayerController中添加简单的生命值逻辑
            Debug.LogWarning("未找到PlayerHealth组件，使用内置简单生命值逻辑");
            player.GetComponent<PlayerController>().AddHealth(healAmount);
        }
    }

    protected override void ApplyEffect(PlayerController player)
    {
        if (player != null)
        {
            HealPlayer(player);
            Debug.Log($"拾取绿色治疗药水！恢复{healAmount}点生命值");

            // 播放特效（可选）
            if (healEffect != null)
            {
                Instantiate(healEffect, player.transform.position, Quaternion.identity, player.transform);
            }
        }
    }
}