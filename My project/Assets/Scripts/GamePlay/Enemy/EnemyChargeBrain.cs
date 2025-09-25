// Assets/Scripts/Enemy/Behaviours/EnemyChargeBrain.cs
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCarController))]
public class EnemyChargeBrain : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float ramDistance = 10f;     // 돌진 시작 거리
    [SerializeField] private float ramFovDeg = 40f;       // 정면 각도 허용
    [SerializeField] private float ramCooldown = 2.0f;    // 돌진 간격
    [SerializeField] private float aimLeadTime = 0.25f;   // 예측 조준 시간

    private EnemyCarController car;
    private Transform target;
    private float cd;

    void Awake()
    {
        car = GetComponent<EnemyCarController>();
        target = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!target) return;
        cd -= Time.deltaTime;

        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 to = target.position - pos; to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        // 정면 각도 체크
        float ang = Vector3.Angle(fwd, to.normalized);

        // 돌진 조건
        if (dist <= ramDistance && ang <= ramFovDeg && cd <= 0f)
        {
            // 약간의 예측(플레이어 rigidbody 속도 사용)
            var trb = target.GetComponent<Rigidbody>();
            Vector3 aim = target.position;
            if (trb) aim += trb.linearVelocity * aimLeadTime;

            Vector3 dir = (aim - pos); dir.y = 0f;
            dir.Normalize();

            // 강한 스로틀 + 직접 조향(왼/오 스티어)
            float steer = Mathf.Clamp(Vector3.SignedAngle(fwd, dir, Vector3.up) / 45f, -1f, 1f);
            float throttle = 1.0f;

            // 너무 급하면 살짝 감속(충돌 센서는 CarController가 담당)
            if (ang > 25f) throttle = 0.8f;

            car.SetDesired(steer, throttle);
            cd = ramCooldown * 0.5f; // 짧게 유지(충돌/회피와 섞이게)
        }
        // 그 외 구간은 A*가 SetDesired를 계속 밀어주므로 기본 추격 유지
    }
}
