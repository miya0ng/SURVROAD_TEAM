using UnityEngine;

[DisallowMultipleComponent]
public class EnemyGunController : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform muzzle;                 // 반드시 지정
    [SerializeField] private GameObject projectilePrefab;      // 반드시 Rigidbody+EnemyProjectile 포함
    [SerializeField] private ParticleSystem muzzleFx;

    [Header("Firing")]
    public float fireInterval = 2.0f;   // CSV: AttackInterval
    public int damage = 5;              // CSV: AttackDamage
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float inaccuracyDeg = 2f;
    [SerializeField] private float maxRange = 60f;

    // (선택) 풀 사용 시 주석 해제
    // private ObjectPool pool;

    private LivingEntity ownerLE;
    private Collider[] ownerCols;

    void Awake()
    {
        // 필수 바인딩 체크
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

        // 프리팹 구성 예비 점검(한 번만)
        if (!projectilePrefab.GetComponent<Rigidbody>())
            Debug.LogError($"[EnemyGunController] projectilePrefab에 Rigidbody 없음: {projectilePrefab.name}");
        if (!projectilePrefab.GetComponent<EnemyProjectile>())
            Debug.LogError($"[EnemyGunController] projectilePrefab에 EnemyProjectile 없음: {projectilePrefab.name}");

        // 풀 사용 시
        // pool = ObjectPool.GetOrCreate(projectilePrefab);

        ownerLE = GetComponentInParent<LivingEntity>();
        ownerCols = ownerLE ? ownerLE.GetComponentsInChildren<Collider>() : null;
    }

    public void ApplySpec(EnemySpec spec)
    {
        damage = Mathf.Max(1, spec.AttackDamage);
        fireInterval = Mathf.Max(0.05f, spec.AttackInterval);
    }

    public void TryFire(Vector3 dirToTarget)
    {
        if (!enabled) return;
        if (!muzzle || !projectilePrefab) return; // 안전망

        // 산포 반영
        Vector3 dir = Quaternion.Euler(0f, Random.Range(-inaccuracyDeg, inaccuracyDeg), 0f) * dirToTarget.normalized;

        FireOne(dir);
    }

    private void FireOne(Vector3 dir)
    {
        // 총구 기준 월드 좌표/회전
        Vector3 spawnPos = muzzle.position;
        Quaternion spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        // ── 생성(풀 또는 인스턴스) ──
        GameObject go = null;
        // if (pool != null) go = pool.Pop(spawnPos, spawnRot);
        // else
        go = Instantiate(projectilePrefab, spawnPos, spawnRot);

        if (!go)
        {
            Debug.LogError("[EnemyGunController] Projectile 생성 실패");
            return;
        }

        // ── 컴포넌트 잡기 ──
        var rb = go.GetComponent<Rigidbody>();
        var proj = go.GetComponent<EnemyProjectile>();

        if (!proj)
        {
            Debug.LogError($"[EnemyGunController] 생성된 Projectile에 EnemyProjectile 없음: {go.name}");
            // if (pool != null) pool.Push(go); else Destroy(go);
            Destroy(go);
            return;
        }
        if (!rb)
        {
            Debug.LogError($"[EnemyGunController] 생성된 Projectile에 Rigidbody 없음: {go.name}");
            // if (pool != null) pool.Push(go); else Destroy(go);
            Destroy(go);
            return;
        }

        // ── 발사체 세팅 ──
        proj.Setup(damage, maxRange);
        //if (ownerLE) proj.SetOwner(ownerLE, ownerLE.teamId, ownerCols);

        // Rigidbody 초기화 후 발사 속도 부여
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;

        // 안전상 위치/회전 한 번 더 보정(풀 사용 시 첫 프레임 0,0,0 플래시 방지)
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
