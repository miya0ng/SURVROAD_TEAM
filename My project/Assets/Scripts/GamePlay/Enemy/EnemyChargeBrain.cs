// Assets/Scripts/Enemy/Behaviours/EnemyChargeBrain.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCarController))]
public class EnemyChargeBrain : MonoBehaviour
{
    [Header("Specs from CSV")]
    public float attackInterval = 3f;     // CSV: AttackInterval (돌진 주기, 초)
    public float attackRange = 15f;       // CSV: AttackRange or 사정거리 (기본값)
    public float maxSpeed = 25f;          // CSV: MaxSpeed
    public float handling = 7f;           // CSV: Handling

    [Header("Charge Pattern")]
    [SerializeField] private float reverseDuration = 0.8f;  // 후진 시간
    [SerializeField] private float chargeDuration = 1.5f;   // 돌진 시간
    [SerializeField] private float ramFovDeg = 40f;         // 돌진 가능 각도

    [Header("Prediction")]
    [SerializeField] private float aimLeadTime = 0.25f;     // 예측 선행 시간

    private EnemyCarController car;
    private Transform target;
    private Rigidbody targetRb;

    // 상태 머신
    private enum ChargeState { Idle, Reversing, Charging }
    private ChargeState state = ChargeState.Idle;
    private float stateTimer;
    private float attackTimer;

    void Awake()
    {
        car = GetComponent<EnemyCarController>();
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (target) targetRb = target.GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        state = ChargeState.Idle;
        stateTimer = 0f;
        attackTimer = 0f; // 활성화 직후 바로 공격 준비
    }

    public void ApplySpec(float speed, float handle, float interval, float range)
    {
        maxSpeed = Mathf.Max(1f, speed);
        handling = Mathf.Max(0.5f, handle);
        attackInterval = Mathf.Max(0.5f, interval);
        attackRange = Mathf.Max(5f, range);

        if (car)
        {
            car.maxSpeed = maxSpeed;
            car.handling = handling;
        }
    }

    void Update()
    {
        if (!target || !car) return;

        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 toTarget = target.position - pos;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist < 0.001f) return;

        float angle = Vector3.Angle(fwd, toTarget.normalized);

        switch (state)
        {
            case ChargeState.Idle:
                UpdateIdle(dist, angle, toTarget);
                break;
            case ChargeState.Reversing:
                UpdateReversing();
                break;
            case ChargeState.Charging:
                UpdateCharging(toTarget);
                break;
        }
    }

    private void UpdateIdle(float dist, float angle, Vector3 toTarget)
    {
        attackTimer += Time.deltaTime;

        if (dist <= attackRange && angle <= ramFovDeg && attackTimer >= attackInterval)
        {
            state = ChargeState.Reversing;
            stateTimer = 0f;
            attackTimer = 0f;
        }
        else
        {
            Vector3 dir = toTarget.normalized;
            float steer = Mathf.Clamp(Vector3.SignedAngle(transform.forward, dir, Vector3.up) / 45f, -1f, 1f);
            car.SetDesired(steer, 1f);
        }
    }

    private void UpdateReversing()
    {
        stateTimer += Time.deltaTime;

        Vector3 toTarget = (target.position - transform.position);
        toTarget.y = 0f;
        Vector3 reverseDir = -toTarget.normalized;

        float targetAngle = Mathf.Atan2(reverseDir.x, reverseDir.z) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.y;
        float deltaAngle = Mathf.DeltaAngle(currentAngle, targetAngle);
        float steer = Mathf.Clamp(deltaAngle / 45f, -1f, 1f);

        var rb = car.GetComponent<Rigidbody>();
        if (rb)
        {
            Vector3 backwardVel = -transform.forward * (maxSpeed * 0.4f);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, backwardVel, Time.deltaTime * 5f);
        }

        car.SetDesired(steer, 0f);

        if (stateTimer >= reverseDuration)
        {
            state = ChargeState.Charging;
            stateTimer = 0f;
        }
    }

    private void UpdateCharging(Vector3 toTarget)
    {
        stateTimer += Time.deltaTime;

        Vector3 aim = target.position;
        if (targetRb)
        {
            aim += targetRb.linearVelocity * aimLeadTime;
        }

        Vector3 dir = (aim - transform.position);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            car.SetDesired(0f, 1f);
        }
        else
        {
            dir.Normalize();

            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;
            float deltaAngle = Mathf.DeltaAngle(currentAngle, targetAngle);
            float steer = Mathf.Clamp(deltaAngle / 45f, -1f, 1f);

            float angle = Vector3.Angle(transform.forward, dir);
            float throttle = (angle > 25f) ? 0.8f : 1.0f;

            car.SetDesired(steer, throttle);
        }

        if (stateTimer >= chargeDuration)
        {
            state = ChargeState.Idle;
            stateTimer = 0f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 fwd = transform.forward;
        Vector3 left = Quaternion.Euler(0, -ramFovDeg, 0) * fwd;
        Vector3 right = Quaternion.Euler(0, ramFovDeg, 0) * fwd;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, left * attackRange);
        Gizmos.DrawRay(transform.position, right * attackRange);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"State: {state}\nTimer: {stateTimer:F1}s\nAttack: {attackTimer:F1}/{attackInterval:F1}s"
        );
    }
#endif
}