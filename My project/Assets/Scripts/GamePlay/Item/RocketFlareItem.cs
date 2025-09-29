using UnityEngine;

public class RocketFlareItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        float dmg = itemData.Damage; // CSV 500
        var cam = Camera.main;
        if (cam)
        {
            foreach (var e in EnemyQuery.GetEnemiesInView(cam))
            {
                var le = e.GetComponent<LivingEntity>();
                if (le) le.OnDamage(dmg, player.GetComponent<LivingEntity>());
            }
        }
        Destroy(gameObject, 0.5f);
    }
}