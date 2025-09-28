using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private EnemyDriver driver;      // CSV 스펙 주체
    [SerializeField] private AStarCarMotor motor;     // 길찾기+조향
    [SerializeField] private EnemyGunController gun;  // 사격(있으면)

    [Header("Tuning")]
    [SerializeField] private float shootRange = 30f;  // 사격 개시 거리(장비/밸런스에 맞춰 조정)
    [SerializeField] private LayerMask losMask = ~0;  // 시야 차단 레이어

    private Transform target;
    private EnemySpec spec;
    private bool hasSpec;
    private bool armed;           // 풀에서 꺼낸 직후 1프레임 암세이프
    private int spawnedFrame;

    void Reset()
    {
        driver = GetComponent<EnemyDriver>();
        gun = GetComponentInChildren<EnemyGunController>();
        motor = GetComponent<AStarCarMotor>();
    }

    void Awake()
    {
        if (!driver) driver = GetComponent<EnemyDriver>();
        if (!gun) gun = GetComponentInChildren<EnemyGunController>();
        if (!motor) motor = GetComponent<AStarCarMotor>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        target = playerObj ? playerObj.transform : null;
    }

    void OnEnable()
    {
        // 풀에서 pop된 직후 한 프레임은 좌표/의존성 안정화 대기
        armed = false;
        spawnedFrame = Time.frameCount;
        // 다음 프레임에 무장
        StartCoroutine(ArmNextFrame());
    }

    System.Collections.IEnumerator ArmNextFrame()
    {
        yield return null;  // 1 프레임 대기
        armed = true;
    }

    void Start()
    {
        // 스펙 캐싱 및 의존성 주입
        hasSpec = (driver && driver.TryGetSpec(out spec));

        if (motor && hasSpec)
        {
            var car = GetComponent<EnemyCarController>();
            if (car && target)
                motor.Bind(car, target);
        }

        if (gun && hasSpec)
        {
            // CSV 스펙 -> 총기 반영
            gun.ApplySpec(Mathf.Max(1, spec.AttackDamage), Mathf.Max(0.05f, spec.AttackInterval));
            // 필요 시 총구 소켓 재매핑: gun.RemapMuzzle(driver.MuzzleSocket);
        }
    }

    void Update()
    {
        if (!armed) return;                 // 첫 프레임 방지
        if (!target || !driver) return;

        // 스펙이 런타임에 교체될 수 있으면 매 프레임 갱신 (비용 적음)
        if (driver.TryGetSpec(out var newSpec))
        {
            if (!hasSpec || !newSpec.Equals(spec))
            {
                spec = newSpec;
                hasSpec = true;
                if (gun) gun.ApplySpec(Mathf.Max(1, spec.AttackDamage), Mathf.Max(0.05f, spec.AttackInterval));
            }
        }

        switch (spec.AttackType)
        {
            case EnemyAttackType.Charge:
                // 별도 처리 없음: motor가 추격
                break;

            case EnemyAttackType.Gun:
                HandleShooting();
                break;

            case EnemyAttackType.Suicide:
                HandleSuicide();
                break;
        }
    }

    void HandleShooting()
    {
        if (!gun) return;

        Vector3 selfPos = transform.position;
        Vector3 targetPos = target.position;

        // 사거리 체크 (수평 기준)
        Vector3 flat = targetPos - selfPos;
        flat.y = 0f;
        if (flat.sqrMagnitude > shootRange * shootRange) return;

        // 시야(Line of Sight) 체크: muzzle이 있으면 muzzle 기준으로 더 정확하게
        Vector3 origin = gun ? (gun.transform.position) : (selfPos + Vector3.up * 0.6f);
        Vector3 dest = targetPos + Vector3.up * 0.6f;

        bool blocked = Physics.Linecast(origin, dest, losMask, QueryTriggerInteraction.Ignore);
        if (blocked) return;

        // 발사 지시: 실제 쿨타임/발사 타이밍은 Gun이 관리(CanFire/interval 내부)
        gun.TickAutoFireToward(targetPos);
    }

    void HandleSuicide()
    {
        // 단순 근접 폭발
        Vector3 to = target.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude <= 6f * 6f)
        {
            var exploder = GetComponent<Exploder>();
            if (exploder) exploder.Trigger(driver.CollisionDamageAsInt(), transform);
            else GetComponent<LivingEntity>()?.OnDamage(999999f, driver);
        }
    }

    // (선택) 디버그용
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
#endif
}
