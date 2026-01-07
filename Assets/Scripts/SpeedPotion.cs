using UnityEngine;

public class SpeedPotion : Potion
{
    [Header("速度药水效果")]
    public ParticleSystem speedEffect;

    protected override void ApplyEffect(PlayerController player)
    {
        if (player != null)
        {
            player.StartSpeedBoost(speedMultiplier, duration);
            Debug.Log($"角色获得速度药水，{duration}秒内速度提升{speedMultiplier}倍");

            // 显示加速提示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEffectTip($"获得加速！{duration}秒内速度×{speedMultiplier}", UIManager.Instance.speedBoostColor);
            }

            // 播放特效（可选）
            if (speedEffect != null)
            {
                Instantiate(speedEffect, player.transform.position, Quaternion.identity, player.transform);
            }
        }
    }
}