using UnityEngine;

public class LookBackPotion : Potion
{
    [Header("回头药水特效")]
    public ParticleSystem chargeEffect;

    protected override void ApplyEffect(PlayerController player)
    {
        if (player != null)
        {
            player.AddLookbackCharge(lookBackCharges);
            Debug.Log($"拾取红色回头药水！增加{lookBackCharges}次回头机会");

            // 播放特效（可选）
            if (chargeEffect != null)
            {
                Instantiate(chargeEffect, player.transform.position, Quaternion.identity, player.transform);
            }
        }
    }
}