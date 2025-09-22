using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빠른 총알용: 스윕 충돌(Raycast/SphereCast) + 라인 렌더러 세그먼트 제한 + 풀링 대응
/// Player/Enemy 공용. ownerTeam으로 아군피해 방지.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Runtime")]
    private TeamId ownerTeam;
    private LivingEntity owner;

    private float lifeTime = 1f;
    private float damage = 10f;
    private float speed = 30f;

    [Header("Collision")]
    [SerializeField] private LayerMask hitMask = ~0;   // 필요시 Enemy 레이어만 등으로 설정
    [SerializeField] private float castRadius = 0f;    // SphereCast 반경(0이면 RayCast)
    [SerializeField] private float spawnIgnoreTime = 0.05f; // 스폰 직후 자기충돌 방지

    [Header("Piercing")]
    [SerializeField] private bool piercing = false;
    [SerializeField] private int maxPierce = 5;
    private int pierceCount = 0;
    private HashSet<Collider> hitOnce = new();

    [Header("Tracer")]
    [SerializeField] private LineRenderer tracer; // 없으면 null 허용
    [SerializeField] private int maxTracerPoints = 60; // 1초 60fps 가정
    private readonly List<Vector3> points = new();

    // State
    private Vector3 dir;
    private Vector3 prevPos;
    private float elapsed;
    private float spawnTime;
    private bool running;
    private Coroutine co;

    // 풀링 훅(선택)
    public System.Action<Bullet> OnDespawnToPool; // 외부(풀)에서 할당하면 Push로 반환

    void Awake()
    {
        if (!tracer) tracer = GetComponent<LineRenderer>();
        // Collider는 Trigger 권장 (또는 Rigidbody 연동)
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    /// <summary>
    /// Weapon에서 호출
    /// </summary>
    public void Init(float speed, float lifeTime, float damage, TeamId team, LivingEntity owner,
                     bool piercing = false, int maxPierce = 1)
    {
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.damage = damage;
        this.ownerTeam = team;
        this.owner = owner;
        this.piercing = piercing;
        this.maxPierce = Mathf.Max(1, maxPierce);

        elapsed = 0f;
        pierceCount = 0;
        hitOnce.Clear();
        running = true;
        spawnTime = Time.time;

        dir = transform.forward; // 발사 방향
        prevPos = transform.position;

        // 트레이서 초기화
        if (tracer)
        {
            points.Clear();
            AddTracerPoint(prevPos);
        }

        // 기존 코루틴 정리 후 시작
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoFlight());
    }

    private IEnumerator CoFlight()
    {
        while (running && elapsed < lifeTime)
        {
            float dt = Time.deltaTime;
            Vector3 nextPos = prevPos + dir * speed * dt;

            // 스윕 충돌 체크
            if (castRadius > 0f)
            {
                if (Physics.SphereCast(prevPos, castRadius, dir, out RaycastHit hit, (nextPos - prevPos).magnitude, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (HandleHit(hit.collider, hit.point))
                    {
                        // 비관통이면 즉시 종료, 관통이면 계속(충돌 지점까지 위치 고정)
                        nextPos = hit.point;
                        if (!piercing) { Despawn(); yield break; }
                    }
                }
            }
            else
            {
                if (Physics.Raycast(prevPos, dir, out RaycastHit hit, (nextPos - prevPos).magnitude, hitMask, QueryTriggerInteraction.Ignore))
                {
                    if (HandleHit(hit.collider, hit.point))
                    {
                        nextPos = hit.point;
                        if (!piercing) { Despawn(); yield break; }
                    }
                }
            }

            transform.position = nextPos;
            prevPos = nextPos;

            if (tracer) AddTracerPoint(nextPos);

            elapsed += dt;
            yield return null;
        }

        Despawn();
    }

    /// <summary>
    /// 트리거 충돌은 보조 용도(느린 탄/넓은 콜라이더 등)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!running) return;

        // 스폰 직후 자기 콜라이더 접촉 방지
        if (Time.time - spawnTime < spawnIgnoreTime) return;

        // 스윕에서 이미 처리되었을 수 있으니 체크
        HandleHit(other, transform.position);
        if (!piercing) Despawn();
    }

    private bool HandleHit(Collider other, Vector3 hitPoint)
    {
        if (!running || other == null) return false;

        // 자기 자신/오너 무시
        if (owner && other.gameObject == owner.gameObject) return false;

        // 중복 히트 방지
        if (hitOnce.Contains(other)) return false;

        // LivingEntity 검사
        if (other.TryGetComponent<LivingEntity>(out var entity))
        {
            // 아군 무시
            if (entity.teamId == ownerTeam) return false;

            // 데미지 인터페이스 호환
            bool damaged = false;
            if (other.TryGetComponent<IDamagable>(out var dmg))
            {
                dmg.OnDamage(damage);
                damaged = true;
            }

            if (damaged)
            {
                hitOnce.Add(other);
                pierceCount++;
                if (piercing && pierceCount < maxPierce) return true; // 관통 계속
                return true;
            }
        }

        // 벽/지형 등에 부딪히면 바로 끝
        if (((1 << other.gameObject.layer) & hitMask.value) != 0)
        {
            // 필요시 스파크 VFX 등
            return true;
        }

        return false;
    }

    private void AddTracerPoint(Vector3 p)
    {
        points.Add(p);
        if (points.Count > maxTracerPoints)
            points.RemoveAt(0);

        tracer.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            tracer.SetPosition(i, points[i]);
    }

    private void Despawn()
    {
        running = false;

        // 트레이서 초기화(풀 반환 대비)
        if (tracer)
        {
            tracer.positionCount = 0;
            points.Clear();
        }

        // 풀링 우선
        if (OnDespawnToPool != null)
        {
            OnDespawnToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        // 코루틴/상태 정리 (풀 복귀 시 안전)
        if (co != null) StopCoroutine(co);
        running = false;
        if (tracer)
        {
            tracer.positionCount = 0;
            points.Clear();
        }
        hitOnce.Clear();
    }
}
