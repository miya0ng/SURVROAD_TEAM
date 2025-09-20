using UnityEngine;

public class HealItem : ItemBase
{
    public float healAmount = 20f;

    protected override void Collect(GameObject player)
    {
        var hp = player.GetComponent<PlayerBehaviour>();
        if (hp != null)
            hp.Heal(itemData.Damage); // CSV °ª ÂüÁ¶
        Destroy(gameObject);
    }
}
