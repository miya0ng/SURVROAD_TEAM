using UnityEngine;

public class AuraDamageSpawn : MonoBehaviour, IProjectileSpawn
{
    [SerializeField] private float radiusMultiplier = 1f;
    [SerializeField] private LayerMask enemyMask;

    private readonly Collider[] hits = new Collider[64];

    public void Spawn(WeaponContext ctx)
    {
        float r = ctx.Level.AttackRange * radiusMultiplier;
        int n = Physics.OverlapSphereNonAlloc(transform.position, r, hits, enemyMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            var le = hits[i].GetComponentInParent<LivingEntity>();
            if (le == null || le.teamId == ctx.TeamId) continue;

            le.OnDamage(ctx.Level.MaxDamage, null);
        }
        if (ctx.FireFx) ctx.FireFx.Play();
    }
}