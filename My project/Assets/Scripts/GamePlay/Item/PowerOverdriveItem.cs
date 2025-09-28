using UnityEngine;

public class PowerOverdriveItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var status = player.GetComponent<PlayerStatusEffects>();
        if (status)
        {
            float mul = 1.5f; // “공속 50% 증가”
            float sec = itemData.Duration > 0 ? itemData.Duration : 4f;
            status.ApplyAttackSpeedBuff(mul, sec);
        }
        Destroy(gameObject);
    }
}