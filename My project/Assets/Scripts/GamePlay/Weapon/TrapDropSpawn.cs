using UnityEngine;

public class TrapDropSpawn : MonoBehaviour, IProjectileSpawn
{
    [Header("Where to drop")]
    [SerializeField] private Transform dropPoint;

    [Header("Ground snap")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float castHeight = 5f;
    [SerializeField] private float maxCastDown = 200f;
    [SerializeField] private float yOffset = 0.05f;
    [Header("Fallback")]
    [SerializeField] private bool fallbackToOwnerPosIfNoHit = true;

    public void Spawn(WeaponContext ctx)
    {
        Transform src = dropPoint ? dropPoint : ctx.Owner.transform;

        Vector3 origin = src.position + Vector3.up * castHeight;
        Vector3 spawnPos;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castHeight + maxCastDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            spawnPos = hit.point + Vector3.up * yOffset;
        }
        else
        {
            if (!fallbackToOwnerPosIfNoHit) return;
            spawnPos = src.position;
        }

        var obj = Instantiate(ctx.Level.prefab, spawnPos, Quaternion.identity);

        if (obj.TryGetComponent<TrapMine>(out var mine))
        {
            mine.Init(ctx.Owner, ctx.TeamId);
        }

        if (ctx.FireFx) ctx.FireFx.Play();
    }

#if UNITY_EDITOR
    // 디버깅에 도움되는 기즈모
    private void OnDrawGizmosSelected()
    {
        Transform src = dropPoint ? dropPoint : transform;
        Vector3 origin = src.position + Vector3.up * castHeight;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + Vector3.down * (castHeight + maxCastDown));
        Gizmos.DrawWireSphere(origin, 0.05f);
    }
#endif
}
