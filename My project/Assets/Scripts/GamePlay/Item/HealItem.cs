using UnityEngine;

public class HealItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var hp = player.GetComponent<PlayerBehaviour>();
        if (hp != null)
            hp.Heal(itemData.Damage); // CSV °ª ÂüÁ¶
        Destroy(gameObject);
    }
}