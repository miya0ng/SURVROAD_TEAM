// Assets/Scripts/Enemy/Behaviours/EnemyGunBrain.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCarController))]
public class EnemyGunBrain : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyGunController gun;
    [SerializeField] private LayerMask losMask;

    [Header("Ranges")]
    [SerializeField] private float preferMin = 14f;   // 선호 사거리 하한
    [SerializeField] private float preferMax = 24f;   // 선호 사거리 상한
    [SerializeField] private float shootRange = 30f;  // 사격 최대

    [Header("Motion")]
    [SerializeField] private float orbitStrength = 0.55f;   // 원운동 성분
    [SerializeField] private float strafeJitter = 0.5f;     // 좌우 가감
    [SerializeField] private float aimThrottle = 0.65f;     // 조준 시 스로틀 감소

    private EnemyCarController car;
    private Transform target;
    private float jitterSign = 1f;
    private float jitterT;

    void Awake()
    {
        car = GetComponent<EnemyCarController>();
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!gun) gun = GetComponentInChildren<EnemyGunController>();
    }

    private void Start()
    {
        var drv = GetComponent<EnemyDriver>();
        if (drv != null && gun != null && drv.TryGetSpec(out var s))
            gun.ApplySpec(s);
    }

    void Update()
    {
        if (!target) return;

        Vector3 pos = transform.position;
        Vector3 to = target.position - pos; to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        // 원운동 방향(좌/우) 약간 랜덤 전환
        jitterT -= Time.deltaTime;
        if (jitterT <= 0f)
        {
            jitterT = Random.Range(0.7f, 1.6f);
            jitterSign = Random.value < 0.5f ? -1f : 1f;
        }

        // 사거리 유지를 위한 스로틀/스티어 보정
        float throttle = 1f;
        float steer = 0f;

        // 기본적으로 A*가 경로 추격, 여기에 궤도 성분을 얹는다(직접 조향)
        // 궤도 성분: 목표에 대한 수직 방향(좌/우)
        Vector3 right = Vector3.Cross(Vector3.up, to.normalized);
        Vector3 orbitDir = (right * orbitStrength * jitterSign);

        // A*가 forward를 이미 잡아주고 있으니, 여기선 미세 조향만 넣는다
        float orbitSteer = Vector3.SignedAngle(transform.forward, (to.normalized + orbitDir).normalized, Vector3.up) / 45f;
        steer = Mathf.Clamp(orbitSteer + (strafeJitter * jitterSign * 0.15f), -1f, 1f);

        // 선호 사거리 유지를 위해 스로틀 가감
        if (dist < preferMin) throttle = 0.5f;      // 살짝 후퇴 느낌(회전 + 감속)
        else if (dist > preferMax) throttle = 1.0f; // 좀 더 접근
        else throttle = aimThrottle;                // 사선 유지하며 조준

        car.SetDesired(steer, throttle);

        // 사격
        if (gun && dist <= shootRange)
        {
            bool blocked = Physics.Linecast(
                transform.position + Vector3.up * 0.6f,
                target.position + Vector3.up * 0.6f,
                losMask, QueryTriggerInteraction.Ignore);

            if (!blocked)
            {
                Vector3 dir = (target.position - transform.position);
                gun.TryFire(dir.normalized);
            }
        }
    }
}
