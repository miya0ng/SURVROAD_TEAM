// Assets/Scripts/Enemy/Behaviours/EnemySuicideCarBrain.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCarController))]
public class EnemySuicideCarBrain : MonoBehaviour
{
    [Header("Approach (zigzag)")]
    [SerializeField] private float zigzagAmp = 3f;
    [SerializeField] private float zigzagFreq = 1.2f;

    [Header("Ignite & Blast")]
    [SerializeField] private float igniteDistance = 8f; // 점화 시작
    [SerializeField] private float burstThrottle = 1.0f;
    [SerializeField] private float burstSteerBias = 0.0f; // 0이면 정면, 필요시 약간 좌/우
    [SerializeField] private int damageOnExplode = 40;

    private EnemyCarController car;
    private Transform target;
    private Exploder exploder;
    private float t;
    private bool ignited;

    void Awake()
    {
        car = GetComponent<EnemyCarController>();
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        exploder = GetComponent<Exploder>();
    }

    void OnEnable()
    {
        t = 0f;
        ignited = false;
    }

    void Update()
    {
        if (!target) return;
        t += Time.deltaTime;

        Vector3 pos = transform.position;
        Vector3 to = target.position - pos; to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        // 점화 전: 지그재그로 접근
        if (!ignited)
        {
            Vector3 dir = to.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            Vector3 zigzag = right * Mathf.Sin(t * zigzagFreq) * zigzagAmp;

            Vector3 steerVec = (dir + zigzag.normalized * 0.35f).normalized;
            float steer = Mathf.Clamp(Vector3.SignedAngle(transform.forward, steerVec, Vector3.up) / 45f, -1f, 1f);
            float throttle = Mathf.Lerp(0.7f, 1.0f, Mathf.InverseLerp(18f, 40f, dist)); // 멀수록 조금 더 빠르게

            car.SetDesired(steer, throttle);

            if (dist <= igniteDistance)
                ignited = true;

            return;
        }

        // 점화 후: 직선 가속 → 폭발
        {
            Vector3 dir = to.normalized;
            float steer = Mathf.Clamp(Vector3.SignedAngle(transform.forward, dir, Vector3.up) / 45f + burstSteerBias, -1f, 1f);
            car.SetDesired(steer, burstThrottle);

            // 근접 시 폭발
            if (dist <= Mathf.Max(3f, igniteDistance * 0.5f))
            {
                if (exploder)
                {
                    // CSV 스펙에서 공격력을 받아왔다면 spec.AttackDamage 전달
                    // 없다면 Brain에 직렬화한 damageOnExplode 사용
                    var driver = GetComponent<EnemyEntity>();
                    if (driver != null && driver.TryGetSpec(out var spec))
                        exploder.Trigger(spec, transform);   // 오버로드 사용 (동일 시그니처도 OK)
                    else
                        exploder.Trigger(damageOnExplode, transform);
                }
                else GetComponent<LivingEntity>()?.OnDamage(999999f, null);
            }
        }
    }
}
