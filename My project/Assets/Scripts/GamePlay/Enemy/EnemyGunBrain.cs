// Assets/Scripts/Enemy/Behaviours/EnemyGunBrain.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCarController))]
public class EnemyGunBrain : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyGunController gun;
    [SerializeField] private LayerMask losMask = ~0;

    [Header("Ranges")]
    [SerializeField] private float preferMin = 14f;   // 선호 사거리 하한
    [SerializeField] private float preferMax = 24f;   // 선호 사거리 상한
    [SerializeField] private float shootRange = 30f;  // 사격 최대 거리

    [Header("Motion")]
    [SerializeField] private float orbitStrength = 0.55f; // 원운동 성분 가중치
    [SerializeField] private float strafeJitter = 0.5f;   // 좌우 가감(미세 요동)
    [SerializeField] private float aimThrottle = 0.65f;   // 조준 시 스로틀(중거리에서 감속)

    private EnemyCarController car;
    private Transform target;

    // 원운동/지터
    private float jitterSign = 1f;
    private float jitterT;

    // 풀 팝 직후 첫 프레임 암세이프
    private bool armed;

    void Reset()
    {
        gun = GetComponentInChildren<EnemyGunController>();
    }

    void Awake()
    {
        car = GetComponent<EnemyCarController>();
        if (!gun) gun = GetComponentInChildren<EnemyGunController>();
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) target = p.transform;
    }

    void OnEnable()
    {
        // 풀에서 Pop된 직후 좌표/참조 안정화를 위해 1프레임 대기
        armed = false;
        StartCoroutine(ArmNextFrame());
    }

    System.Collections.IEnumerator ArmNextFrame()
    {
        yield return null; // 한 프레임 대기
        armed = true;
    }

    void Start()
    {
        // CSV 스펙 → 총기 반영
        var drv = GetComponent<EnemyDriver>();
        if (drv != null && gun != null && drv.TryGetSpec(out var s))
        {
            gun.ApplySpec(Mathf.Max(1, s.AttackDamage), Mathf.Max(0.05f, s.AttackInterval));
        }
    }

    void Update()
    {
        if (!armed || !target || !car) return;

        Vector3 pos = transform.position;
        Vector3 to = target.position - pos;
        to.y = 0f;

        float dist = to.magnitude;
        if (dist < 0.001f) return;

        // ===== 원운동 방향(좌/우) 랜덤 전환 =====
        jitterT -= Time.deltaTime;
        if (jitterT <= 0f)
        {
            jitterT = Random.Range(0.7f, 1.6f);
            jitterSign = Random.value < 0.5f ? -1f : 1f;
        }

        // ===== 사거리 유지를 위한 조향/스로틀 =====
        float throttle = 1f;
        float steer = 0f;

        // 목표에 대한 오른쪽 방향 벡터(수평)
        Vector3 right = Vector3.Cross(Vector3.up, to.normalized);
        Vector3 orbitDir = right * orbitStrength * jitterSign;

        // A*가 추격 중이라 가정하고, 여기선 미세 조향만 더해준다.
        Vector3 desiredDir = (to.normalized + orbitDir).normalized;
        float orbitSteer = Vector3.SignedAngle(transform.forward, desiredDir, Vector3.up) / 45f;
        steer = Mathf.Clamp(orbitSteer + (strafeJitter * jitterSign * 0.15f), -1f, 1f);

        // 선호 사거리대에 따라 스로틀 가감
        if (dist < preferMin) throttle = 0.5f;   // 살짝 후퇴 느낌(회전 + 감속)
        else if (dist > preferMax) throttle = 1.0f;   // 좀 더 접근
        else throttle = aimThrottle;

        car.SetDesired(steer, throttle);

        // ===== 사격 =====
        if (gun && dist <= shootRange)
        {
            // LOS: 가능하면 총(=총구 자식)의 위치 기준
            Vector3 origin = gun.transform.position;
            Vector3 dest = target.position + Vector3.up * 0.6f;

            bool blocked = Physics.Linecast(origin, dest, losMask, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                // 쿨타임/발사 타이밍은 Gun이 관리 → Brain은 지시만
                gun.TickAutoFireToward(target.position);
            }
        }
    }
}
