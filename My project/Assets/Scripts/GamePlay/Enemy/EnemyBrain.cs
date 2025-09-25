// Assets/Scripts/Enemy/EnemyBrain.cs
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    [SerializeField] private EnemyDriver driver;     // CSV 스펙을 들고 있는 주체
    [SerializeField] private AStarCarMotor motor;    // 길찾기+조향
    [SerializeField] private EnemyGunController gun; // 사격(있으면)

    [Header("Tuning")]
    [SerializeField] private float shootRange = 30f;
    [SerializeField] private LayerMask losMask;

    private Transform target;
    private EnemySpec spec;
    private float cd;

    void Awake()
    {
        if (!driver) driver = GetComponent<EnemyDriver>();
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Start()
    {
        if (motor && driver && driver.TryGetSpec(out spec))
        {
            motor.Bind(GetComponent<EnemyCarController>(), target);
        }
        // Gun 세팅
        if (gun && driver && driver.TryGetSpec(out spec))
        {
            gun.damage = Mathf.Max(1, spec.AttackDamage);
            gun.fireInterval = Mathf.Max(0.05f, spec.AttackInterval);
        }
    }

    void Update()
    {
        if (!driver || !driver.TryGetSpec(out spec) || !target) return;

        switch (spec.AttackType)
        {
            case EnemyAttackType.Charge:
                // 특수 행동 없음: 그냥 최단 경로 쫓기. 
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
        cd -= Time.deltaTime;

        Vector3 to = target.position - transform.position;
        float dist = to.magnitude;
        if (dist <= shootRange && cd <= 0f)
        {
            bool blocked = Physics.Linecast(
                transform.position + Vector3.up * 0.6f,
                target.position + Vector3.up * 0.6f,
                losMask, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                gun.TryFire(to.normalized);
                cd = gun.fireInterval;
            }
        }
    }

    void HandleSuicide()
    {
        // 간단 버전: 일정 거리 이하면 폭발.
        Vector3 to = target.position - transform.position;
        if (to.magnitude <= 6f)
        {
            var exploder = GetComponent<Exploder>();
            if (exploder) exploder.Trigger(driver.CollisionDamageAsInt(), transform);
            else GetComponent<LivingEntity>()?.OnDamage(999999f, driver);
        }
    }
}
