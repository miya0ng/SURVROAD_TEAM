using UnityEngine;

public class ReinforcedShieldItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var status = player.GetComponent<PlayerStatusEffects>();
        if (status) status.ApplyInvulnerability(itemData.Duration > 0 ? itemData.Duration : 3f);
        Destroy(gameObject);
    }
}