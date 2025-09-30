// Assets/Scripts/Common/Combat/Bullet.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빠른 총알용: 스윕 충돌(Raycast/SphereCast)
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
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float castRadius = 0f;                 // 0이면 RayCast, >0이면 SphereCast
    [SerializeField] private float spawnIgnoreTime = 0.05f;         // 스폰 직후 자기충돌 방지

    [Header("Tracer")]
    [SerializeField] private LineRenderer tracer;  // null 가능
    [SerializeField] private int maxTracerPoints = 60; // 1초 60fps 가정
    private readonly List<Vector3> points = new();

    // State
    private Vector3 dir;
    private Vector3 prevPos;
    private float elapsed;
    private float spawnTime;
    private bool running;
    private Coroutine co;

    private  WeaponContext ctx;
    // 풀링 훅(선택)
    public System.Action<Bullet> OnDespawnToPool;

    void Awake()
    {
        if (!tracer) tracer = GetComponent<LineRenderer>();
        var col = GetComponent<Collider>();
        col.isTrigger = true; // 스윕 주이므로 Trigger 권장
    }

    /// <summary>공용 초기화</summary>
    public void Init(
        WeaponContext ctx, LivingEntity owner)
    {
        this.speed = ctx.Level.BulletSpeed;
        this.lifeTime = ctx.Level.Duration;
        this.damage = ctx.Level.MaxDamage;
        this.ownerTeam = ctx.TeamId;
        this.owner = owner;
        this.ctx = ctx;

        elapsed = 0f;
        running = true;
        spawnTime = Time.time;

        dir = transform.forward;
        prevPos = transform.position;

        if (tracer)
        {
            points.Clear();
            AddTracerPoint(prevPos);
        }

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoFlight());
    }

    private IEnumerator CoFlight()
    {
        while (running && elapsed < lifeTime)
        {
            float dt = Time.deltaTime;
            Vector3 nextPos = prevPos + dir * speed * dt;

            // 스윕 충돌
            float dist = (nextPos - prevPos).magnitude;
            if (dist > 0f)
            {
                bool hitSomething = false;
                RaycastHit hit;

                if (castRadius > 0f)
                {
                    if (Physics.SphereCast(prevPos, castRadius, dir, out hit, dist, hitMask, QueryTriggerInteraction.Ignore))
                        hitSomething = HandleHit(hit.collider, hit.point);
                }
                else
                {
                    if (Physics.Raycast(prevPos, dir, out hit, dist, hitMask, QueryTriggerInteraction.Ignore))
                        hitSomething = HandleHit(hit.collider, hit.point);
                }

                if (hitSomething)
                {
                    nextPos = hit.point;
                    transform.position = nextPos;
                    Despawn(); 
                    yield break; 
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

    private void OnTriggerEnter(Collider other)
    {
        if (!running) return;
        if (Time.time - spawnTime < spawnIgnoreTime) return; // 스폰 직후 자기/총구 충돌 무시

        if (HandleHit(other, transform.position))
            Despawn();
    }

    private bool HandleHit(Collider other, Vector3 hitPoint)
    {
        if (!running || other == null) return false;
        if (owner && other.gameObject == owner.gameObject) return false; // 자신 무시

        // LivingEntity 데미지
        if (other.TryGetComponent(out LivingEntity entity))
        {
            if (entity.teamId == ownerTeam) return false; // 아군무시

            bool damaged = false;

            // IDamagable 우선
            if (other.TryGetComponent<IDamagable>(out var dmg))
            {
                dmg.OnDamage(damage);
                damaged = true;
                if (TryGetComponent<ExplodeAttack>(out var rocketWeapon))
                {
                    rocketWeapon.Explode(ctx);//TODO 
                }
            }
            else
            {
                // 없으면 LivingEntity 직접
                entity.OnDamage(damage, owner);
                damaged = true;
            }

            if (damaged)
            {
                return true;
            }
        }

        // 벽/지형 등
        if (((1 << other.gameObject.layer) & hitMask.value) != 0)
            return true;

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
        if (tracer) { tracer.positionCount = 0; points.Clear(); }

        if (OnDespawnToPool != null) OnDespawnToPool(this);
        else Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        running = false;
        if (tracer) { tracer.positionCount = 0; points.Clear(); }
    }
}
