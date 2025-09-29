using UnityEngine;

public class TurboBoosterItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var booster = player.GetComponent<PlayerStatusEffects>();
        if (booster)
        {
            float duration = (itemData != null && itemData.Duration > 0f) ? itemData.Duration : 5f;
            float cooldown = 1f;
            booster.UnlockTurbo(duration, cooldown);
        }
        Destroy(gameObject);
    }
}