using UnityEngine;

public class TurboBoosterItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        AudioManager.I.PlaySFX("GetItem");
        var status = player.GetComponent<PlayerStatusEffects>();
        if (status)
        {
            float effectWindow = (itemData != null && itemData.Duration > 0f) ? itemData.Duration : 5f;
            float itemCooldown = 0.2f;
            float itemDuration = 3f;

            status.GrantTurboSpecWindow(effectWindow, itemDuration, itemCooldown);
        }

        Destroy(gameObject);
    }
}
