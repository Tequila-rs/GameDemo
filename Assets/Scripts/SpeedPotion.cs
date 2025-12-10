using UnityEngine;

public class SpeedPotion : Potion
{
    [Header("速度药水特效")]
    public ParticleSystem speedEffect;

    protected override void ApplyEffect(PlayerController player)
    {
        if (player != null)
        {
            player.StartSpeedBoost(speedMultiplier, duration);
            Debug.Log($"拾取蓝色速度药水！{duration}秒内速度提升至{speedMultiplier}倍");

            // 播放特效（可选）
            if (speedEffect != null)
            {
                Instantiate(speedEffect, player.transform.position, Quaternion.identity, player.transform);
            }
        }
    }
}