using UnityEngine;

public class TurboBoosterItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var status = player.GetComponent<PlayerStatusEffects>();
        if (status)
        {
            // 아이템 효과: 5초 동안 쿨 0.2초 / 지속 3초
            float effectWindow = (itemData != null && itemData.Duration > 0f) ? itemData.Duration : 5f;
            float itemCooldown = 0.2f;
            float itemDuration = 3f;

            status.GrantTurboSpecWindow(effectWindow, itemDuration, itemCooldown);
        }

        Destroy(gameObject);
    }
}
