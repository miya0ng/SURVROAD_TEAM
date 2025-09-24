// Assets/Scripts/Enemy/Combat/GunController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// </summary>
[DisallowMultipleComponent]
public class EnemyGunController : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private ParticleSystem muzzleFx;

    [Header("Firing")]
    [SerializeField] private float fireInterval = 2.0f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float projectileSpeed = 40f;
    [SerializeField] private float inaccuracyDeg = 2f;
    [SerializeField] private float maxRange = 60f;

    [Header("Burst/Auto")]
    [SerializeField] private bool useBurst = false;
    [SerializeField] private int shotsPerBurst = 3;
    [SerializeField] private float burstGap = 0.12f;
    [SerializeField] private bool autoFire = true;

    [Header("Checks")]
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private LayerMask losMask = ~0;
    [SerializeField] private float minDotToFire = -1f;

    [Header("Aim Assist")]
    [SerializeField] private bool useLeadShot = true;
    [SerializeField] private float leadMaxSeconds = 1.0f;

    [Header("Pooling (optional)")]
    [SerializeField] private bool useLocalPool = true;
    [SerializeField] private int prewarmCount = 16;

    // ���� ����
    private EnemyBehaviourBase behaviour;
    private float nextFireTime;
    private readonly Queue<GameObject> localPool = new();

    void Awake()
    {
        behaviour = GetComponent<EnemyBehaviourBase>();
        if (muzzle == null) Debug.LogWarning("[GunController] muzzle ������");
        if (projectilePrefab == null) Debug.LogWarning("[GunController] projectilePrefab ������");

        // CSV �Ķ���� �ڵ� �ݿ�(������ ��)
        if (behaviour != null)
        {
            TrySyncFromBehaviour();
        }

        if (useLocalPool && projectilePrefab != null)
        {
            for (int i = 0; i < prewarmCount; i++)
                localPool.Enqueue(CreateBulletInstance());
        }
    }

    void OnEnable()
    {
        nextFireTime = Time.time + Random.Range(0f, fireInterval * 0.5f);
    }

    void Update()
    {
        if (!autoFire) return;
        TryFire();
    }

    private void TrySyncFromBehaviour()
    {
        try
        {
            var t = behaviour.GetType();
            var baseT = typeof(EnemyBehaviourBase);

            var attackIntervalField = baseT.GetField("attackInterval",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (attackIntervalField != null)
            {
                float csvInterval = Mathf.Max(0.05f, (float)attackIntervalField.GetValue(behaviour));
                fireInterval = csvInterval;
            }

            var attackDamageField = baseT.GetField("attackDamage",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (attackDamageField != null)
            {
                int csvDmg = (int)attackDamageField.GetValue(behaviour);
                if (csvDmg > 0) damage = csvDmg;
            }
        }
        catch { /* ���� */ }
    }

    public void SetAttackParams(int dmg, float interval)
    {
        if (dmg > 0) damage = dmg;
        if (interval > 0f) fireInterval = interval;
    }

    public void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (muzzle == null || projectilePrefab == null || behaviour == null) return;

        var target = behaviour ? behaviour.GetComponent<EnemyBehaviourBase>().transform : null;
        target = behaviour != null ? behaviour.GetComponent<EnemyBehaviourBase>().transform : null; // dummy to avoid warning

    
        var targetTrField = typeof(EnemyBehaviourBase).GetField("target",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy);

        Transform tgt = targetTrField != null ? (Transform)targetTrField.GetValue(behaviour) : null;
        if (tgt == null) return;

        Vector3 fireOrigin = muzzle.position;
        Vector3 targetPos = tgt.position;
        if (Vector3.Distance(fireOrigin, targetPos) > maxRange) return;

        if (requireLineOfSight)
        {
            Vector3 dirLOS = (targetPos - fireOrigin).normalized;
            if (Physics.Raycast(fireOrigin, dirLOS, out RaycastHit hit, maxRange, losMask))
            {
                if (hit.transform != tgt && !hit.transform.IsChildOf(tgt))
                    return; 
            }
        }

        if (minDotToFire > -0.999f)
        {
            Vector3 fw = muzzle.forward;
            Vector3 to = (targetPos - fireOrigin).normalized;
            if (Vector3.Dot(fw, to) < minDotToFire) return;
        }

        Vector3 aimDir = ComputeAimDirection(fireOrigin, tgt);

        if (useBurst && shotsPerBurst > 1)
            StartCoroutine(BurstRoutine(aimDir, shotsPerBurst));
        else
            FireOne(aimDir);

        nextFireTime = Time.time + fireInterval;
    }

    private IEnumerator BurstRoutine(Vector3 baseDir, int count)
    {
        for (int i = 0; i < count; i++)
        {
            FireOne(baseDir);
            if (i < count - 1)
                yield return new WaitForSeconds(burstGap);
        }
    }

    private void FireOne(Vector3 baseDir)
    {
        if (muzzle == null || projectilePrefab == null) return;

        Vector3 dir = ApplySpread(baseDir, inaccuracyDeg);

        GameObject go = SpawnBullet(muzzle.position, Quaternion.LookRotation(dir, Vector3.up));

        if (muzzleFx) muzzleFx.Play();

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir * projectileSpeed;
        }

        //1) IProjectile
        if (go.TryGetComponent<IProjectile>(out var ip))
        {
            ip.Init(damage, projectileSpeed, dir);
        }
        // 2) DamageOnHit
        var doh = go.GetComponent<DamageOnHit>();
        if (doh != null) doh.SetDamage(damage);
    }

    private GameObject SpawnBullet(Vector3 pos, Quaternion rot)
    {
        if (!useLocalPool || localPool.Count == 0)
            return Instantiate(projectilePrefab, pos, rot);

        var go = localPool.Dequeue();
        if (!go)
        {
            return Instantiate(projectilePrefab, pos, rot);
        }
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    private GameObject CreateBulletInstance()
    {
        var go = Instantiate(projectilePrefab);
        go.SetActive(false);

        var returner = go.GetComponent<PoolReturner>();
        if (!returner) returner = go.AddComponent<PoolReturner>();
        returner.onReturn = () =>
        {
            go.SetActive(false);
            localPool.Enqueue(go);
        };
        return go;
    }

    private Vector3 ComputeAimDirection(Vector3 fireOrigin, Transform tgt)
    {
        Vector3 targetPos = tgt.position;
        Vector3 targetVel = Vector3.zero;

        if (useLeadShot && projectileSpeed > 0.1f)
        {
            var trb = tgt.GetComponent<Rigidbody>();
            if (trb != null) targetVel = trb.linearVelocity;

            Vector3 toTarget = targetPos - fireOrigin;
            float a = Vector3.Dot(targetVel, targetVel) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(toTarget, targetVel);
            float c = Vector3.Dot(toTarget, toTarget);
            float t = SolveInterceptTime(a, b, c);
            if (t > 0f) t = Mathf.Min(t, leadMaxSeconds);

            if (t > 0f)
                targetPos += targetVel * t;
        }

        return (targetPos - fireOrigin).sqrMagnitude > 0.0001f
            ? (targetPos - fireOrigin).normalized
            : transform.forward;
    }

    private static float SolveInterceptTime(float a, float b, float c)
    {
        float disc = b * b - 4f * a * c;
        if (disc < 0f || Mathf.Abs(a) < 1e-6f) return 0f;

        float sqrt = Mathf.Sqrt(disc);
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        if (t1 > 0f && t2 > 0f) return Mathf.Min(t1, t2);
        if (t1 > 0f) return t1;
        if (t2 > 0f) return t2;
        return 0f;
    }

    private static Vector3 ApplySpread(Vector3 dir, float degrees)
    {
        if (degrees <= 0.01f) return dir.normalized;
        Quaternion q = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Random.onUnitSphere);
        return (q * dir).normalized;
    }
}

public class PoolReturner : MonoBehaviour
{
    public System.Action onReturn;

    public void Return()
    {
        onReturn?.Invoke();
    }
}

public interface IProjectile
{
    void Init(int damage, float speed, Vector3 dir);
}

public class DamageOnHit : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float lifeTime = 5f;

    private float t;

    void OnEnable() { t = 0f; }

    void Update()
    {
        t += Time.deltaTime;
        if (t > lifeTime)
        {
            var pr = GetComponent<PoolReturner>();
            if (pr) pr.Return(); else Destroy(gameObject);
        }
    }

    public void SetDamage(int d) => damage = d;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

        var le = collision.gameObject.GetComponent<LivingEntity>();
        if (le) le.OnDamage(damage, null);

        var pr = GetComponent<PoolReturner>();
        if (pr) pr.Return(); else Destroy(gameObject);
    }
}
