using UnityEngine;

public class HealPotion : Potion
{
    [Header("治疗药水效果")]
    public ParticleSystem healEffect;

    // 通用的玩家加血逻辑（兼容有无PlayerHealth组件）
    private void HealPlayer(PlayerController player)
    {
        // 优先获取PlayerHealth组件
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(healAmount);
        }
        else
        {
            // 备用方案：直接调用PlayerController的加血逻辑
            Debug.LogWarning("未找到PlayerHealth组件，使用备用加血逻辑");
            player.GetComponent<PlayerController>().AddHealth(healAmount);
        }
    }

    protected override void ApplyEffect(PlayerController player)
    {
        if (player != null)
        {
            HealPlayer(player);
            Debug.Log($"角色获得治疗药水，恢复{healAmount}生命值");

            // 显示加血提示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEffectTip($"获得治疗！+{healAmount}生命值", UIManager.Instance.healColor);
            }

            // 播放特效（可选）
            if (healEffect != null)
            {
                Instantiate(healEffect, player.transform.position, Quaternion.identity, player.transform);
            }
        }
    }
}