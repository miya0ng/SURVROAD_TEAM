// Assets/Scripts/Enemy/EnemyDriver.cs
using UnityEngine;
using Pathfinding; // Seeker를 쓰는 A* 모터가 이 네임스페이스 사용

[RequireComponent(typeof(EnemyCarController))]
public class EnemyDriver : LivingEntity
{
    [Header("Bindings")]
    [SerializeField] private EnemyCarController car;
    private Transform target;                       // (기존 주석 처리 제거: 필드 유지)
    [SerializeField] private EnemyGunController gun;

    [Header("Spec Setup")]
    [SerializeField] private int enemyId = 51101;

    [Header("Combat")]
    [SerializeField] private float shootRange = 30f;
    [SerializeField] private LayerMask losMask;

    [Header("Death FX")]
    [SerializeField] private GameObject deathVfxPrefab;   // 죽을 때 생성할 VFX 프리팹
    //[SerializeField] private AudioClip deathSfx;          // 선택: 사운드
    //[SerializeField, Range(0f, 1f)] private float deathSfxVolume = 0.9f;
    [SerializeField] private Transform vfxAnchor;         // 선택: 이 위치 기준(없으면 transform)
    [SerializeField] private bool usePoolForDeathVfx = true;

    private ItemManager itemManager;
    private EnemySpec spec;                         // (필드 추가: 내부 전용, 시그니처 영향 없음)
    private float shootCd;

    // A* 주행용 (동일 컴포넌트에서 찾아 바인딩, 공개 API 변경 없음)
    private AStarCarMotor motor;

    protected override void Awake()
    {
        base.Awake();
        if (!target) target = GameObject.FindGameObjectWithTag("Player")?.transform;
        var im = GameObject.FindGameObjectWithTag("ItemManager");
        if (im) itemManager = im.GetComponent<ItemManager>();

        if (!car) car = GetComponent<EnemyCarController>();
        motor = GetComponent<AStarCarMotor>();

        // 이미 스포너에서 주입했다면 생략
        if (spec.Id != 0) return;

        var table = DataTableManger.Get<EnemyDataTable>(EnemyDataTable.EnemyTableId);
        if (table == null)
        {
            // 폴백(씬 부트스트랩 타이밍 불일치 대비)
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
    void OnEnable()
    {
        shootCd = 0f;
        if (spec.Id != 0)
        {
            maxHp = spec.Durability;
            curHp = maxHp;
        }
        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (car && target) car.Bind(target);
        if (motor && car && target) motor.Bind(car, target);
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
        if (!target) return;

        // 타입 분기: 시그니처 변경 없이 EnemyDriver 안에서 처리
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
                // 특수행동 없음: A* 모터가 있으면 알아서 추격, 없으면 기존 Bind 주행
                break;
        }
    }
    public void ApplySpec(EnemySpec s)
    {
        spec = s;

        maxHp = spec.Durability;
        curHp = maxHp;

        if (!car) car = GetComponent<EnemyCarController>();
        if (car)
        {
            car.maxSpeed = spec.MaxSpeed;
            car.accel = spec.Accel;
            car.handling = Mathf.Max(0.5f, spec.Handling);
        }

        if (gun)
        {
            gun.damage = Mathf.Max(1, spec.AttackDamage);
            gun.fireInterval = Mathf.Max(0.1f, spec.AttackInterval);
        }
    }

    public void SetEnemyId(int id) => enemyId = id;
    public void SetTarget(Transform t)
    {
        target = t;
        if (!car) car = GetComponent<EnemyCarController>();
        if (car && target) car.Bind(target);

        var motor = GetComponent<AStarCarMotor>();
        if (motor && car && target) motor.Bind(car, target);
    }
    void HandleGun()
    {
        if (!gun) return;
        shootCd -= Time.deltaTime;

        Vector3 to = (target.position - transform.position);
        float dist = to.magnitude;
        if (dist <= shootRange && shootCd <= 0f)
        {
            bool blocked = Physics.Linecast(
                transform.position + Vector3.up * 0.6f,
                target.position + Vector3.up * 0.6f,
                losMask, QueryTriggerInteraction.Ignore
            );
            if (!blocked)
            {
                gun.TryFire(to.normalized);
                shootCd = gun.fireInterval;
            }
        }
    }

    void HandleSuicide()
    {
        if (!target) return;
        // 간단: 근접 시 폭발(Exploder 있으면 사용)
        Vector3 to = target.position - transform.position;
        if (to.magnitude <= 6f)
        {
            var exploder = GetComponent<Exploder>();
            if (exploder) exploder.Trigger(Mathf.RoundToInt(spec.AttackDamage), transform);
            else GetComponent<LivingEntity>()?.OnDamage(999999f, this);
        }
    }

    static bool InLayerMask(GameObject go, LayerMask mask)
    {
        return (mask.value & (1 << go.layer)) != 0;
    }

    void OnCollisionEnter(Collision c)
    {
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
        // VFX/SFX는 반드시 좌표를 먼저 캡처
        Vector3 pos = (vfxAnchor ? vfxAnchor : transform).position;
        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);

        SpawnDeathFx(pos, rot);

        if (itemManager) itemManager.DropFromEnemy(pos);

        // 원래 흐름
        base.Die(killer);
    }

    private ObjectPool deathVfxPool;
    private void SpawnDeathFx(Vector3 pos, Quaternion rot)
    {
        if (!deathVfxPrefab) return;

        GameObject fx;
        if (usePoolForDeathVfx && deathVfxPool != null)
            fx = deathVfxPool.Pop(pos, rot);
        else
            fx = Instantiate(deathVfxPrefab, pos, rot);
    }

    public bool TryGetSpec(out EnemySpec s) { s = spec; return spec.Id != 0; }
    public int CollisionDamageAsInt() => Mathf.RoundToInt(spec.CollisionDamage);
}
