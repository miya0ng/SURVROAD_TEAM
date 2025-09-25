// 파일명: EnemyBullet.cs
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyBullet : MonoBehaviour
{
    public static EnemyBullet I { get; private set; }

    [System.Serializable]
    public class Settings
    {
        public int capacity = 1024;
        public float defaultSpawnIgnoreTime = 0.05f;
        public float maxLifeSeconds = 5f;        // 안전 상한
        public float outOfBounds = 2000f;        // 너무 멀어지면 강제 제거
    }
    public Settings settings = new();

    struct Bullet
    {
        public bool active;
        public Vector3 pos, dir;
        public float speed, lifeLeft, damage, castRadius, spawnTime, spawnIgnoreTime;
        public TeamId ownerTeam;
        public LivingEntity owner;
        public LayerMask hitMask;
        public int trailIndex;
    }

    [Header("Trail Pool (optional)")]
    [SerializeField] private TrailRenderer trailPrefab;
    [SerializeField] private Transform trailParent;
    [SerializeField] private int trailPrewarm = 64;

    readonly List<Bullet> bullets = new();
    readonly Queue<int> free = new();

    readonly List<TrailRenderer> trails = new();
    readonly Queue<int> freeTrails = new();

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        bullets.Capacity = settings.capacity;
        for (int i = 0; i < settings.capacity; i++) { bullets.Add(new Bullet()); free.Enqueue(i); }

        if (trailPrefab)
        {
            for (int i = 0; i < trailPrewarm; i++)
            {
                var t = Instantiate(trailPrefab, trailParent);
                t.gameObject.SetActive(false);
                trails.Add(t);
                freeTrails.Enqueue(i);
            }
        }
    }

    public int SpawnProjectile(
        Vector3 pos, Vector3 dir, float speed, float lifeSeconds, float damage,
        float castRadius, TeamId ownerTeam, LivingEntity owner, LayerMask hitMask,
        float spawnIgnoreTime = -1f, bool withTrail = true)
    {
        if (free.Count == 0) return -1;
        int idx = free.Dequeue();

        lifeSeconds = Mathf.Clamp(lifeSeconds, 0.01f, settings.maxLifeSeconds); // 안전 상한

        var b = bullets[idx];
        b.active = true;
        b.pos = pos;
        b.dir = dir.normalized;
        b.speed = speed;
        b.lifeLeft = lifeSeconds;
        b.damage = damage;
        b.castRadius = castRadius;
        b.ownerTeam = ownerTeam;
        b.owner = owner;
        b.hitMask = hitMask;
        b.spawnTime = Time.time;
        b.spawnIgnoreTime = (spawnIgnoreTime >= 0f) ? spawnIgnoreTime : settings.defaultSpawnIgnoreTime;
        b.trailIndex = withTrail ? RentTrail(pos) : -1;
        bullets[idx] = b;
        return idx;
    }

    int RentTrail(Vector3 pos)
    {
        if (!trailPrefab) return -1;
        int idx = freeTrails.Count > 0 ? freeTrails.Dequeue() : trails.Count;
        TrailRenderer t = (idx < trails.Count) ? trails[idx] : Instantiate(trailPrefab, trailParent);
        if (idx >= trails.Count) trails.Add(t);
        t.Clear();
        t.transform.position = pos;
        t.gameObject.SetActive(true);
        return idx;
    }
    void ReturnTrail(int idx)
    {
        if (idx < 0 || idx >= trails.Count) return;
        var t = trails[idx];
        t.gameObject.SetActive(false);
        freeTrails.Enqueue(idx);
    }

    void Despawn(int i)
    {
        var b = bullets[i];
        if (b.trailIndex >= 0) ReturnTrail(b.trailIndex);
        b.active = false;
        bullets[i] = b;
        free.Enqueue(i);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < bullets.Count; i++)
        {
            if (!bullets[i].active) continue;
            var b = bullets[i];

            // life/거리 안전 가드
            if (b.lifeLeft <= 0f || Mathf.Abs(b.pos.x) > settings.outOfBounds || Mathf.Abs(b.pos.z) > settings.outOfBounds)
            { Despawn(i); continue; }

            Vector3 next = b.pos + b.dir * (b.speed * dt);
            float dist = (next - b.pos).magnitude;

            bool hitSomething = false;
            RaycastHit hit = new RaycastHit();

            bool ignore = Time.time - b.spawnTime < b.spawnIgnoreTime;
            if (!ignore && dist > 0f)
            {
                if (b.castRadius > 0f)
                    hitSomething = Physics.SphereCast(b.pos, b.castRadius, b.dir, out hit, dist, b.hitMask, QueryTriggerInteraction.Ignore);
                else
                    hitSomething = Physics.Raycast(b.pos, b.dir, out hit, dist, b.hitMask, QueryTriggerInteraction.Ignore);
            }

            if (hitSomething)
            {
                TryDamage(hit.collider, b.damage, b.ownerTeam, b.owner);
                if (b.trailIndex >= 0) trails[b.trailIndex].transform.position = hit.point;
                Despawn(i);
                continue;
            }

            b.pos = next;
            if (b.trailIndex >= 0) trails[b.trailIndex].transform.position = next;

            b.lifeLeft -= dt;
            bullets[i] = b;
        }
    }

    static void TryDamage(Collider col, float damage, TeamId ownerTeam, LivingEntity owner)
    {
        if (col.TryGetComponent(out LivingEntity le))
        {
            if (le.teamId == ownerTeam) return;
            if (col.TryGetComponent<IDamagable>(out var dmg)) dmg.OnDamage(damage);
            else le.OnDamage(damage, owner);
        }
    }

    // 디버그/응급 도구
    public int CountActive()
    {
        int n = 0; for (int i = 0; i < bullets.Count; i++) if (bullets[i].active) n++;
        return n;
    }
    public void ClearAll()
    {
        for (int i = 0; i < bullets.Count; i++) if (bullets[i].active) Despawn(i);
    }
}
