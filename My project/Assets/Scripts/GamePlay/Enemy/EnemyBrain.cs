using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBrain : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private EnemyEntity entity;
    [SerializeField] private AStarCarMotor motor;
    [SerializeField] private EnemyCarController car;
    [SerializeField] private EnemyGunController gun;

    [Header("Tuning")]
    [SerializeField] private float shootRange = 30f;  // 사격 개시 거리
    [SerializeField] private float suicideRange = 6f;
    [SerializeField] private LayerMask blockView = ~0;  // 시야 차단 레이어

    private Transform target;
    private EnemySpec spec;
    private bool hasSpec;

    void Reset()
    {
        entity = GetComponent<EnemyEntity>();
        gun = GetComponentInChildren<EnemyGunController>();
        motor = GetComponent<AStarCarMotor>();
    }

    void Awake()
    {
        if (!entity) entity = GetComponent<EnemyEntity>();
        if (!gun) gun = GetComponentInChildren<EnemyGunController>();
        if (!motor) motor = GetComponent<AStarCarMotor>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        target = playerObj ? playerObj.transform : null;
    }

    void OnEnable()
    {
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
        // 스펙 캐싱 및 의존성 주입
        hasSpec = (entity && entity.TryGetSpec(out spec));

        if (motor && hasSpec)
        {
            var car = GetComponent<EnemyCarController>();
            if (car && target)
                motor.Bind(car, target);
        }

        if (gun && hasSpec)
        {
            gun.ApplySpec(Mathf.Max(1, spec.AttackDamage), Mathf.Max(0.05f, spec.AttackInterval));
        }

        if (!target)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (car && target) car.Bind(target);
        if (motor && car && target) motor.Bind(car, target);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
#endif
}
