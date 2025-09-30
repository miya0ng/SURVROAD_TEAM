using UnityEngine;

[DisallowMultipleComponent]
public class EnemyGunController : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ParticleSystem muzzleFx;

    [Header("Firing Spec")]
    public float fireInterval = 2.0f;   // CSV: AttackInterval
    public int damage = 5;              // CSV: AttackDamage
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float inaccuracyDeg = 2f;
    [SerializeField] private float maxRange = 60f;

    [Header("Owner (optional)")]
    [SerializeField] private LivingEntity ownerLE;

    private ObjectPool pool;
    private Collider[] ownerCols;
    private float nextFireTime;

    void Awake()
    {
        if (!muzzle)
        {
            Debug.LogError($"[EnemyGunController] muzzle 미할당 on {name}");
            enabled = false; return;
        }
        if (!projectilePrefab)
        {
            Debug.LogError($"[EnemyGunController] projectilePrefab 미할당 on {name}");
            enabled = false; return;
        }
        if (!projectilePrefab.GetComponent<Rigidbody>())
            Debug.LogError($"[EnemyGunController] projectilePrefab에 Rigidbody 없음: {projectilePrefab.name}");
        if (!projectilePrefab.GetComponent<EnemyProjectile>())
            Debug.LogError($"[EnemyGunController] projectilePrefab에 EnemyProjectile 없음: {projectilePrefab.name}");

        pool = ObjectPool.GetOrCreate(projectilePrefab);

        if (!ownerLE) ownerLE = GetComponentInParent<LivingEntity>();
        ownerCols = ownerLE ? ownerLE.GetComponentsInChildren<Collider>() : null;

        nextFireTime = Time.time + Random.Range(0f, fireInterval); // 스타거
    }

    public void ApplySpec(int dmg, float interval)
    {
        damage = Mathf.Max(1, dmg);
        fireInterval = Mathf.Max(0.05f, interval);
    }

    public void RemapMuzzle(Transform newMuzzle)
    {
        if (newMuzzle) muzzle = newMuzzle;
    }

    public bool CanFire() => Time.time >= nextFireTime && enabled && gameObject.activeInHierarchy;

    public void TickAutoFireToward(Vector3 worldTarget)
    {
        if (!CanFire()) return;

        Vector3 dirToTarget = (worldTarget - muzzle.position);
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude < 0.0001f) return;

        // 산포 반영
        Vector3 dir = Quaternion.Euler(0f, Random.Range(-inaccuracyDeg, inaccuracyDeg), 0f) * dirToTarget.normalized;

        FireOne(dir);
        nextFireTime = Time.time + fireInterval;
    }

    public void FireOne(Vector3 dir)
    {
        // 1) 총구 기준 스폰 좌표/회전
        Vector3 spawnPos = muzzle.position;
        Quaternion spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        // 2) 풀에서 Pop (활성화 전에 위치/회전 세팅됨)
        GameObject go = pool.Pop(spawnPos, spawnRot);
        var rb = go.GetComponent<Rigidbody>();
        var proj = go.GetComponent<EnemyProjectile>();

        if (!rb || !proj)
        {
            Debug.LogError("[EnemyGunController] Projectile 구성(Rigidbody/EnemyProjectile) 누락");
            if (rb) { /* nothing */ }
            // 안전상 제거
            pool.Push(go);
            return;
        }

        // 3) 발사체 세팅
        proj.Setup(damage, maxRange, pool, ownerLE, ownerCols);

        // 4) 물리 발사
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;

        // 풀에서 Pop 직후 한 번 더 보정(원점 플래시 방지용)
        rb.position = spawnPos;
        rb.rotation = spawnRot;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = dir * projectileSpeed;
#else
        rb.velocity = dir * projectileSpeed;
#endif

        if (muzzleFx) muzzleFx.Play();
    }
}