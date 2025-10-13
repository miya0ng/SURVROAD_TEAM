using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class EnemyProjectile : MonoBehaviour, IPoolable
{
    [Header("Specs")]
    [SerializeField] private float baseLife = 4f;
    [SerializeField] private LayerMask hitMask;

    [Header("Hit FX (optional)")]
    [SerializeField] private ParticleSystem hitFx;

    private int damage;
    private float lifeLeft;
    private float maxRange;
    private Vector3 spawnPos;
    private Vector3 lastPos;

    // 풀 & 소유자
    private ObjectPool pool;
    private LivingEntity owner;
    private Collider[] ownerCols;

    // 발사 직후 자가피격 방지용 프레임
    private int bornFrame;

    public void Setup(int dmg, float range, ObjectPool poolRef, LivingEntity ownerLE, Collider[] ownerColliders)
    {
        damage = dmg;
        maxRange = Mathf.Max(1f, range);
        pool = poolRef;
        owner = ownerLE;
        ownerCols = ownerColliders;
    }

    public void OnPoppedFromPool()
    {
        lifeLeft = baseLife;
        spawnPos = transform.position;
        lastPos = spawnPos;
        bornFrame = Time.frameCount;

        // 물리 초기화
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        // 소유자 충돌 무시(짧은 시간 또는 한 프레임)
        if (ownerCols != null && TryGetComponent<Collider>(out var myCol))
        {
            foreach (var oc in ownerCols)
            {
                if (oc && myCol) Physics.IgnoreCollision(myCol, oc, true);
            }
        }
    }

    public void OnPushedToPool()
    {
        // 무시했던 충돌 복구
        if (ownerCols != null && TryGetComponent<Collider>(out var myCol))
        {
            foreach (var oc in ownerCols)
            {
                if (oc && myCol) Physics.IgnoreCollision(myCol, oc, false);
            }
        }
        owner = null;
        ownerCols = null;
        pool = null;
    }

    void OnEnable()
    {
        // Pop 시점에 OnPoppedFromPool에서 대부분 초기화함
        lastPos = transform.position;
        AudioManager.I?.PlaySFX("TurretShoot", transform.position);
    }

    void Update()
    {
        // 수명/사거리 종료
        lifeLeft -= Time.deltaTime;
        if (lifeLeft <= 0f || (transform.position - spawnPos).sqrMagnitude >= maxRange * maxRange)
        {
            ReturnToPool();
            return;
        }

        // 터널링 보정(프레임 간 레이)
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;
        float len = delta.magnitude;
        if (len > 0.0001f)
        {
            if (Physics.Raycast(lastPos, delta.normalized, out var hit, len + 0.05f, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit.collider, hit.point);
                return;
            }
        }
        lastPos = pos;
    }

    void OnCollisionEnter(Collision c)
    {
        // 생성 프레임에는 오작동 방지
        if (Time.frameCount == bornFrame) return;
        OnHit(c.collider, c.GetContact(0).point);
    }

    private void OnHit(Collider col, Vector3 at)
    {
        // 소유자 무시
        if (owner && col && owner.transform.IsChildOf(col.transform)) return;

        // 데미지
        var le = col.GetComponentInParent<LivingEntity>();
        if (le != null)
        {
            le.OnDamage(damage, owner);
        }

        // 히트 FX
        if (hitFx)
        {
            var fx = Instantiate(hitFx, at, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (pool != null) pool.Push(gameObject);
    }
}
