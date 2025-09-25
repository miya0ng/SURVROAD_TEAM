using UnityEngine;

public class HealItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        var hp = player.GetComponent<PlayerBehaviour>();
        if (hp != null)
            hp.Heal(300); // CSV °ª ÂüÁ¶
        Destroy(gameObject);
    }
}