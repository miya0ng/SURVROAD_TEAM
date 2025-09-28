using UnityEngine;

public class StunItem : ItemBase
{
    [SerializeField] float radius = 12f;
    [SerializeField] float force = 40f;

    protected override void Collect(GameObject player)
    {
        float dmg = itemData.Damage; // CSV 30
        var pos = player.transform.position;
        var cols = Physics.OverlapSphere(pos, radius);
        foreach (var c in cols)
        {
            if (!c.attachedRigidbody) continue;
            if (!c.CompareTag("Enemy")) continue;
            c.attachedRigidbody.AddExplosionForce(force, pos, radius, 0.5f, ForceMode.Impulse);

            var le = c.GetComponent<LivingEntity>();
            if (le) le.OnDamage(dmg, player.GetComponent<LivingEntity>());
        }
        Destroy(gameObject);
    }
}