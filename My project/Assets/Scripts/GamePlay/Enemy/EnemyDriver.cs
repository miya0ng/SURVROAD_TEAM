// Assets/Scripts/Enemy/EnemyDriver.cs
using UnityEngine;
using Pathfinding; // Seeker를 쓰는 A* 모터가 이 네임스페이스 사용

[RequireComponent(typeof(EnemyCarController))]
public class EnemyDriver : LivingEntity
{
    [Header("Bindings")]
    [SerializeField] private EnemyCarController car;
    private Transform target;
    [SerializeField] private EnemyGunController gun;

    [Header("Spec Setup")]
    [SerializeField] private int enemyId;

    [Header("Combat")]
    [SerializeField] private float shootRange = 30f;
    [SerializeField] private LayerMask losMask = ~0;

    [Header("Death FX")]
    [SerializeField] private GameObject deathVfxPrefab; // 죽을 때 생성할 VFX 프리팹
    [SerializeField] private Transform vfxAnchor;       // 없으면 this.transform
    [SerializeField] private bool usePoolForDeathVfx = true;

    private ItemManager itemManager;
    private EnemySpec spec;             // 내부 캐시
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

        // 레퍼런스 찾아두기
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) target = p.transform;

        var im = GameObject.FindGameObjectWithTag("ItemManager");
        if (im) itemManager = im.GetComponent<ItemManager>();

        // Death FX 풀 준비(선택)
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
            else
            {
                Debug.LogWarning($"[EnemyDriver] Spec not found: {enemyId}");
                maxHp = curHp = 50;
            }
        }
    }

    void OnEnable()
    {
        if (spec.Id != 0)
        {
            maxHp = spec.Durability;
            curHp = maxHp;
        }

        // 타겟 보정
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        if (car && target) car.Bind(target);
        if (motor && car && target) motor.Bind(car, target);

        armed = false;
        spawnedFrame = Time.frameCount;
        StartCoroutine(ArmNextFrame());
    }

    System.Collections.IEnumerator ArmNextFrame()
    {
        yield return null;
        armed = true;
    }

    void Start()
    {
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (car && target) car.Bind(target);
        if (motor && car && target) motor.Bind(car, target);
    }

    void Update()
    {
        if (!armed) return;
        if (!target) return;

        if (TryGetSpec(out var s) && s.Id != spec.Id)
        {
            spec = s;
            ApplySpec(spec);
        }

        switch (spec.AttackType)
        {
            case EnemyAttackType.Gun:
                HandleGun();
                break;

            case EnemyAttackType.Suicide:
                HandleSuicide();
                break;

            case EnemyAttackType.Charge:
            default:
                // 특수행동 없음: motor/차량이 추격
                break;
        }
    }

    public void ApplySpec(EnemySpec s)
    {
        spec = s;

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

    public void SetTarget(Transform t)
    {
        target = t;
        if (!car) car = GetComponent<EnemyCarController>();
        if (car && target) car.Bind(target);
        if (motor && car && target) motor.Bind(car, target);
    }

    void HandleGun()
    {
        if (!gun || !target) return;

        // 사거리 체크 (수평 거리)
        Vector3 to = target.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > shootRange * shootRange) return;

        // LOS 체크: 총구(없으면 본체 상단) 기준
        Vector3 origin = gun ? gun.transform.position : (transform.position + Vector3.up * 0.6f);
        Vector3 dest = target.position + Vector3.up * 0.6f;

        bool blocked = Physics.Linecast(origin, dest, losMask, QueryTriggerInteraction.Ignore);
        if (blocked) return;

        // 발사 지시 (쿨타임은 Gun 내부에서 판단)
        gun.TickAutoFireToward(target.position);
    }

    void HandleSuicide()
    {
        if (!target) return;

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude <= 6f * 6f)
        {
            var exploder = GetComponent<Exploder>();
            if (exploder) exploder.Trigger(CollisionDamageAsInt(), transform);
            else GetComponent<LivingEntity>()?.OnDamage(999999f, this);
        }
    }

    static bool InLayerMask(GameObject go, LayerMask mask)
        => (mask.value & (1 << go.layer)) != 0;

    void OnCollisionEnter(Collision c)
    {
        // LOS 마스크 대상은 접촉 데미지 제외(환경 등)
        if (InLayerMask(c.collider.gameObject, losMask)) return;

        var le = c.collider.GetComponentInParent<LivingEntity>();
        if (le && le.teamId != this.teamId)
        {
            le.OnDamage(spec.CollisionDamage, this);
        }
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

        // 기본 처리 및 드랍
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
