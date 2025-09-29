using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class TrapMine : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 50f;
    [SerializeField] private float radius = 4f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private bool lineOfSightCheck = false;

    [Header("Behavior (Timer-only)")]
    [SerializeField] private float armingDelay = 0.3f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private bool explodeOnTrigger = true;
    [SerializeField] private bool explodeOnProximity = true;
    [SerializeField] private float proximityPoll = 0.1f;

    [Header("Ground Snap")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float castHeight = 2.5f;
    [SerializeField] private float maxCastDown = 50f;
    [SerializeField] private float yOffset = 0.03f;
    [SerializeField] private float maxSlopeDeg = 55f;

    [Header("FX")]
    [SerializeField] private GameObject explodeVfxPrefab;
    [SerializeField] private float vfxLifetime = 2f;

    private LivingEntity owner;
    private TeamId teamId;

    private bool armed;
    private bool exploded;

    public Action<TrapMine> OnDespawnToPool;

    private readonly Collider[] hits = new Collider[64];
    private Collider myCol;
    private float lifeTimer;
    //private float proxTimer;

    public void Init(LivingEntity owner, TeamId team)
    {
        this.owner = owner;
        this.teamId = team;
    }

    void Awake()
    {
        myCol = GetComponent<Collider>();
        myCol.isTrigger = true;
    }
void OnEnable()
{
    if (groundMask == 0) groundMask = LayerMask.GetMask("Ground"); // 안전 기본값
    SnapToGround();

    exploded = false;
    armed = false;
    lifeTimer = 0f;
    if (armingDelay <= 0f) armed = true;
    else Invoke(nameof(ArmNow), armingDelay);
}

    void OnDisable()
    {
        CancelInvoke();
        armed = false;
        exploded = false;
    }

    private void ArmNow() => armed = true;

    void Update()
    {
        if (exploded) return;

        // �� Ÿ�̸� ��� �ڵ� ����
        if (lifeTime > 0f)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifeTime)
            {
                Explode();
                return;
            }
        }

    }

    // ���� Ground Snap ����������������������������������������������������������������������������������������������������������������������������
    private void SnapToGround()
    {
        Debug.Log("SnaToGround");
        Vector3 origin = transform.position + Vector3.up * castHeight;

        // 1) ����Ʒ� ����ĳ��Ʈ�� ���� ã��
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castHeight + maxCastDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope <= maxSlopeDeg)
            {
                transform.position = hit.point + hit.normal * yOffset;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                return;
            }
        }
    }

    // ���� Damage & Validations ����������������������������������������������������������������������������������������������������������
    private bool HasLineOfSight(LivingEntity target)
    {
        Vector3 from = transform.position + Vector3.up * 0.2f;
        Vector3 to = target.transform.position + Vector3.up * 0.5f;
        if (Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<LivingEntity>() == target;
        }
        return true;
    }

    private bool IsValidTarget(LivingEntity le)
    {
        if (le == null) return false;
        if (le == owner) return false;
        if (le.teamId == teamId) return false;
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!explodeOnTrigger || !armed || exploded) return;

        var le = other.GetComponentInParent<LivingEntity>();
        if (!IsValidTarget(le)) return;
        if (lineOfSightCheck && !HasLineOfSight(le)) return;

        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        int n = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, hitMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var le = hits[i].GetComponentInParent<LivingEntity>();
            if (!IsValidTarget(le)) continue;
            le.OnDamage(damage, owner);
        }

        if (explodeVfxPrefab)
        {
            var vfx = Instantiate(explodeVfxPrefab, transform.position, Quaternion.identity);
            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }

        Despawn();
    }
private void Despawn()
{
    if (OnDespawnToPool != null) { OnDespawnToPool(this); return; }
    // 풀을 안 쓰면 기본 파괴
    gameObject.SetActive(false); // 눈에 보이던 비가시 전환 최소화
}
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 1f);
        Gizmos.DrawWireSphere(transform.position, radius);

        // Ground snap ray
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * castHeight;
        Gizmos.DrawLine(origin, origin + Vector3.down * (castHeight + maxCastDown));
        Gizmos.DrawWireSphere(origin, 0.06f);
    }


#endif
}
