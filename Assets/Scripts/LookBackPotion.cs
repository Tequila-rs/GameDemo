using UnityEngine;

public class LookBackPotion : Potion
{
    [Header("回头药水效果")]
    public ParticleSystem chargeEffect;

    protected override void ApplyEffect(PlayerController player)
    {
        if (player != null)
        {
            player.AddLookbackCharge(lookBackCharges);
            Debug.Log($"角色获得回头药水，增加{lookBackCharges}次回头次数");

            // 显示增加回头次数提示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEffectTip($"获得回头次数！+{lookBackCharges}次", UIManager.Instance.lookBackChargeColor);
            }

            // 播放特效（可选）
            if (chargeEffect != null)
            {
                Instantiate(chargeEffect, player.transform.position, Quaternion.identity, player.transform);
            }
        }
    }
}