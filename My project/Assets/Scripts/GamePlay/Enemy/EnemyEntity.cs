// Assets/Scripts/Enemy/EnemyEntity.cs
using UnityEngine;
using Pathfinding; // Seeker를 쓰는 A* 모터가 이 네임스페이스 사용

[RequireComponent(typeof(EnemyCarController))]
public class EnemyEntity : LivingEntity
{
    [Header("Bindings")]
    [SerializeField] private EnemyCarController car;
    [SerializeField] private EnemyGunController gun;

    [Header("Spec Setup")]
    [SerializeField] private int enemyId;

    [Header("Death FX")]
    [SerializeField] private GameObject deathVfxPrefab; // 죽을 때 생성할 VFX 프리팹
    [SerializeField] private Transform vfxAnchor;       // 없으면 this.transform
    [SerializeField] private bool usePoolForDeathVfx = true;

    private ItemManager itemManager;
    private EnemySpec spec = new EnemySpec();
    private AStarCarMotor motor;

    // 풀 팝 직후 첫 프레임 스킵용
    private bool armed;
    private int spawnedFrame;

    // Death VFX 풀
    private ObjectPool deathVfxPool;
    protected override void Awake()
    {
        base.Awake();

        if (!car) car = GetComponent<EnemyCarController>();
        motor = GetComponent<AStarCarMotor>();

        //var p = GameObject.FindGameObjectWithTag("Player");
        //if (p) target = p.transform;

        var im = GameObject.FindGameObjectWithTag("ItemManager");
        if (im) itemManager = im.GetComponent<ItemManager>();

        if (usePoolForDeathVfx && deathVfxPrefab)
        {
            deathVfxPool = ObjectPool.GetOrCreate(deathVfxPrefab);
        }

        if (spec.Id == 0)
        {
            var table = DataTableManger.Get<EnemyDataTable>(EnemyDataTable.EnemyTableId);
            if (table == null)
            {
                table = new EnemyDataTable();
                table.Load(EnemyDataTable.EnemyTableId);
            }

            if (table != null && table.TryGet(enemyId, out spec))
            {
                ApplySpec(spec);
            }
        }
    }

    protected override void OnEnable()
    {
        if (spec.Id != 0)
        {
            maxHp = spec.Durability;
            curHp = maxHp;
        }

        armed = false;
        spawnedFrame = Time.frameCount;
        StartCoroutine(ArmNextFrame());
    }

    System.Collections.IEnumerator ArmNextFrame()
    {
        yield return null;
        armed = true;
    }

    void Update()
    {
        if (!armed) return;

        if (TryGetSpec(out var s) && s.Id != spec.Id)
        {
            spec = s;
            ApplySpec(spec);
        }
    }

    public void ApplySpec(EnemySpec s)
    {
        spec = s;
        enemyId = s.Id;
        maxHp = spec.Durability;
        curHp = Mathf.Min(curHp, maxHp);

        if (!car) car = GetComponent<EnemyCarController>();
        if (car)
        {
            car.maxSpeed = spec.MaxSpeed;
            car.accel = spec.Accel;
            car.handling = Mathf.Max(0.5f, spec.Handling);
        }

        if (gun)
        {
            gun.ApplySpec(Mathf.Max(1, spec.AttackDamage), Mathf.Max(0.05f, spec.AttackInterval));
        }
    }

    public void SetEnemyId(int id) => enemyId = id;

    static bool InLayerMask(GameObject go, LayerMask mask)
        => (mask.value & (1 << go.layer)) != 0;

    void OnCollisionEnter(Collision c)
    {
        //// LOS 마스크 대상은 접촉 데미지 제외(환경 등)
        //if (InLayerMask(c.collider.gameObject, losMask)) return;

        //var le = c.collider.GetComponentInParent<LivingEntity>();
        //if (le && le.teamId == TeamId.Player)
        //{
        //    le.OnDamage(spec.CollisionDamage, this);
        //}
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        var flash = GetComponentInChildren<HitFlash>();
        if (flash != null) flash.PlayFlash();
    }

    protected override void Die(LivingEntity killer = null)
    {
        Debug.Log("Enemy Die: " + gameObject.name);
        // 좌표/회전 캡처 후 VFX
        Vector3 pos = (vfxAnchor ? vfxAnchor : transform).position;
        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);

        SpawnDeathFx(pos, rot);

        base.Die(killer);
        if (itemManager) itemManager.DropFromEnemy(pos);
    }

    private void SpawnDeathFx(Vector3 pos, Quaternion rot)
    {
        if (!deathVfxPrefab) return;

        if (usePoolForDeathVfx)
        {
            if (deathVfxPool == null)
                deathVfxPool = ObjectPool.GetOrCreate(deathVfxPrefab);

            deathVfxPool.Pop(pos, rot);
        }
        else
        {
            Instantiate(deathVfxPrefab, pos, rot);
        }
    }

    public bool TryGetSpec(out EnemySpec s) { s = spec; return spec.Id != 0; }
    public int CollisionDamageAsInt() => Mathf.RoundToInt(spec.CollisionDamage);
}
