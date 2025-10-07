using System.Collections;
using UnityEngine;

/// <summary>
/// 발사 총알용: 즉시 충돌(Raycast/SphereCast)
/// Player/Enemy 공용. ownerTeam으로 아군판별 가능.
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
    [SerializeField] private float castRadius = 0f;
    [SerializeField] private float spawnIgnoreTime = 0.05f;
    [Header("Tracer - TrailRenderer")]
    [SerializeField] private TrailRenderer tracer;
    [SerializeField] private float trailTime = 0.3f;
    [SerializeField] private float trailWidth = 0.1f;

    private Vector3 dir;
    private Vector3 prevPos;
    private float elapsed;
    private float spawnTime;
    private bool running;
    private Coroutine co;

    private WeaponContext ctx;
    
    public System.Action<Bullet> OnDespawnToPool;

    void Awake()
    {
        if (!tracer) tracer = GetComponent<TrailRenderer>();
        
        if (tracer)
        {
            tracer.time = trailTime;
            tracer.startWidth = trailWidth;
            tracer.endWidth = trailWidth * 0.5f;
            tracer.autodestruct = false;
        }

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void Init(WeaponContext ctx, LivingEntity owner)
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
            tracer.Clear();
            tracer.emitting = true;
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

            // 물리 충돌
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

            elapsed += dt;
            yield return null;
        }

        Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!running) return;
        if (Time.time - spawnTime < spawnIgnoreTime) return;

        if (HandleHit(other, transform.position))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            if (ctx.FireFx != null)
            {
                var fx = Instantiate(ctx.FireFx, hitPoint, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.2f);
            }

            Despawn();
        }
    }

    private bool HandleHit(Collider other, Vector3 hitPoint)
    {
        if (!running || other == null) return false;
        if (owner && other.gameObject == owner.gameObject) return false;

        if (other.TryGetComponent(out LivingEntity entity))
        {
            if (entity.teamId == ownerTeam) return false;

            bool damaged = false;

            if (other.TryGetComponent<IDamagable>(out var dmg))
            {
                dmg.OnDamage(damage);
                damaged = true;
                if (TryGetComponent<ExplodeAttack>(out var rocketWeapon))
                {
                    rocketWeapon.Explode(ctx);
                }
            }
            else
            {
                entity.OnDamage(damage, owner);
                damaged = true;
            }

            if (damaged)
            {
                return true;
            }
        }

        if (((1 << other.gameObject.layer) & hitMask.value) != 0)
            return true;

        return false;
    }

    private void Despawn()
    {
        running = false;
   
        if (tracer)
        {
            tracer.emitting = false;
        }

        if (OnDespawnToPool != null) OnDespawnToPool(this);
        else Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (co != null) StopCoroutine(co);
        running = false;

        if (tracer)
        {
            tracer.Clear();
            tracer.emitting = false;
        }
    }
}