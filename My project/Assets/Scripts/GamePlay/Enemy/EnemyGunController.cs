using UnityEngine;

[DisallowMultipleComponent]
public class EnemyGunController : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ParticleSystem muzzleFx;

    [Header("Firing")]
    public float fireInterval = 5.0f;   // CSV: AttackInterval
    public int damage = 5;              // CSV: AttackDamage
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float inaccuracyDeg = 2f;
    [SerializeField] private float maxRange = 60f;

    // ===== CSV 주입 =====
    /// <summary>
    /// EnemySpec(CSV)에서 공격력/발사간격을 주입한다.
    /// </summary>
    public void ApplySpec(EnemySpec spec)
    {
        // 안전 클램프(발사 간격 최소 보장, 데미지 음수 방지)
        damage = Mathf.Max(1, spec.AttackDamage);
        fireInterval = Mathf.Max(0.05f, spec.AttackInterval);
        // projectileSpeed / maxRange / inaccuracyDeg은 CSV에 없으므로
        // 필요 시 별도 테이블/필드로 주입(지금은 인스펙터 값 유지)
    }

    /// <summary>
    /// 같은 엔티티에 붙은 EnemyDriver로부터 자동으로 스펙을 받아 적용(선택).
    /// 프리팹에서 [ContextMenu]로 테스트 가능.
    /// </summary>
    [ContextMenu("Apply Spec From EnemyDriver")]
    public void SetupFromDriver()
    {
        var drv = GetComponentInParent<EnemyDriver>();
        if (drv != null && drv.TryGetSpec(out var spec))
        {
            ApplySpec(spec);
#if UNITY_EDITOR
            Debug.Log($"[EnemyGunController] Applied from EnemyDriver: dmg={damage}, interval={fireInterval:F2}");
#endif
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("[EnemyGunController] EnemyDriver/Spec not found to apply.");
        }
#endif
    }

    public void TryFire(Vector3 dirToTarget)
    {
        if (!muzzle || !projectilePrefab) return;
        Vector3 dir = Quaternion.Euler(0f, Random.Range(-inaccuracyDeg, inaccuracyDeg), 0f) * dirToTarget.normalized;
        FireOne(dir);
    }

    private void FireOne(Vector3 dir)
    {
        var go = GameObject.Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir, Vector3.up));
        var rb = go.GetComponent<Rigidbody>();
        var proj = go.GetComponent<EnemyProjectile>();
        if (proj) { proj.Setup(damage, maxRange); }

        if (rb)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = dir * projectileSpeed;
        }

        if (muzzleFx) muzzleFx.Play();
    }
}
