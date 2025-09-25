// Assets/Scripts/Enemy/Movement/EnemyCarController.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCarController : MonoBehaviour
{
    [Header("Bindings")]
    public Transform visualRoot;
    public LayerMask obstacleMask;

    [Header("Specs (from CSV)")]
    public float maxSpeed = 20f;
    public float accel = 8f;
    public float handling = 6f;
    public float desiredRadius = 12f;

    [Header("Steering")]
    public float baseTurnDeg = 35f;
    public float brakeDecel = 14f;
    public float cornerSlowFactor = 0.6f;

    [Header("Sensors")]
    public float feelerLen = 6f;
    public float feelerSideAngle = 22f;
    public float sideFeelerLen = 4.5f;
    public float avoidWeight = 1.8f;
    public float pursueWeight = 1.0f;

    private Rigidbody rb;
    private Transform target;

    // === 외부 입력(예: A* 모터/행동AI)이 잠깐 덮어쓰게 하는 버퍼 ===
    float extSteer;          // -1..1
    float extThrottle;       // 0..1
    float extHold = 0f;      // 남은 적용 시간(초)
    const float ExtApplyWindow = 0.15f; // 최근 프레임 입력을 0.15초간 유지

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void Bind(Transform target)
    {
        this.target = target;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 pos = rb.position;
        Vector3 forward = transform.forward;

        // ── 공통: 센서(회피/브레이크 판정) 먼저 계산 ──
        bool needBrake = false;
        Vector3 avoidDir = Vector3.zero;
        if (RayHit(forward, feelerLen, out var hitC)) { avoidDir += Vector3.Reflect(forward, hitC.normal); needBrake = true; }
        Vector3 leftDir = Quaternion.Euler(0f, -feelerSideAngle, 0f) * forward;
        if (RayHit(leftDir, sideFeelerLen, out var hitL)) { avoidDir += Vector3.Reflect(leftDir, hitL.normal); needBrake = true; }
        Vector3 rightDir = Quaternion.Euler(0f, feelerSideAngle, 0f) * forward;
        if (RayHit(rightDir, sideFeelerLen, out var hitR)) { avoidDir += Vector3.Reflect(rightDir, hitR.normal); needBrake = true; }

        float speed = rb.linearVelocity.magnitude;

        // ── 1) 외부 원하는 조향/스로틀이 살아있으면 우선 적용 ──
        if (extHold > 0f)
        {
            extHold -= dt;

            // 속도가 높을수록 조향 제한(언더스티어) — handling으로 응답 조절
            float turnLimit = Mathf.Lerp(baseTurnDeg, baseTurnDeg * 0.33f, Mathf.InverseLerp(0f, maxSpeed, speed));
            float turnDeg = Mathf.Clamp(extSteer, -1f, 1f) * turnLimit;

            // 회전(handling이 크면 더 민감)
            Quaternion q = Quaternion.AngleAxis(turnDeg * handling * dt, Vector3.up);
            rb.MoveRotation(rb.rotation * q);

            // 목표 속도 = maxSpeed * throttle(0..1)
            float targetSpeed = Mathf.Lerp(0f, maxSpeed, Mathf.Clamp01(extThrottle));
            if (needBrake) targetSpeed *= cornerSlowFactor;

            // 가/감속: accel / brakeDecel 반영
            float dv = targetSpeed - speed;
            float a = dv >= 0f ? accel : brakeDecel;
            float newSpeed = Mathf.MoveTowards(speed, targetSpeed, a * dt);

            Vector3 vel = transform.forward * newSpeed;
            rb.linearVelocity = new Vector3(vel.x, rb.linearVelocity.y, vel.z);

            if (visualRoot)
                visualRoot.forward = Vector3.Slerp(visualRoot.forward, rb.linearVelocity.sqrMagnitude > 0.1f ? rb.linearVelocity : transform.forward, 0.2f);

            return; // 외부 입력을 썼으므로 내부 추격 로직은 스킵
        }

        // ── 2) 내부 추격/회피(기존 로직 유지) ──
        if (target == null) return;

        // pursuit: 간단 예측
        Vector3 toT = target.position - pos;
        float dist = toT.magnitude;
        Vector3 targetVel = Vector3.zero;
        var tRb = target.GetComponent<Rigidbody>();
        if (tRb) targetVel = tRb.linearVelocity;

        float lookAhead = Mathf.Clamp(dist / Mathf.Max(1f, speed), 0.05f, 1.2f);
        Vector3 futurePos = target.position + targetVel * lookAhead;

        Vector3 desired = (futurePos - pos);
        desired.y = 0f;
        if (desired.magnitude > 0.001f)
        {
            if (desired.magnitude < desiredRadius * 1.2f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, desired.normalized);
                desired += right * Mathf.Sign(Vector3.SignedAngle(forward, desired, Vector3.up)) * 0.35f * (1f - desired.magnitude / (desiredRadius * 1.2f));
            }
        }

        Vector3 pursueDir = desired.sqrMagnitude > 0.000001f ? desired.normalized : forward;

        Vector3 steerDir = (avoidDir != Vector3.zero)
            ? (pursueDir * pursueWeight + avoidDir.normalized * avoidWeight).normalized
            : pursueDir;

        float turnLimit2 = Mathf.Lerp(baseTurnDeg, baseTurnDeg * 0.33f, Mathf.InverseLerp(0f, maxSpeed, speed));
        float turn2 = Mathf.Clamp(Vector3.SignedAngle(forward, steerDir, Vector3.up), -turnLimit2, turnLimit2);

        Quaternion q2 = Quaternion.AngleAxis(turn2 * handling * dt, Vector3.up);
        rb.MoveRotation(rb.rotation * q2);

        float targetSpeed2 = maxSpeed;
        if (needBrake) targetSpeed2 *= cornerSlowFactor;

        float dv2 = targetSpeed2 - speed;
        float a2 = dv2 >= 0f ? accel : brakeDecel;
        float newSpeed2 = Mathf.MoveTowards(speed, targetSpeed2, a2 * dt);

        Vector3 vel2 = transform.forward * newSpeed2;
        rb.linearVelocity = new Vector3(vel2.x, rb.linearVelocity.y, vel2.z);

        if (visualRoot)
            visualRoot.forward = Vector3.Slerp(visualRoot.forward, rb.linearVelocity.sqrMagnitude > 0.1f ? rb.linearVelocity : transform.forward, 0.2f);
    }

    bool RayHit(Vector3 dir, float len, out RaycastHit hit)
    {
        return Physics.Raycast(rb.position + Vector3.up * 0.5f, dir, out hit, len, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    // === 외부에서 주는 “의도”를 차량 스펙에 맞게 즉시 적용 가능하도록 큐잉 ===
    public void SetDesired(float steerInput, float throttleInput)
    {
        // 입력 정규화
        extSteer = Mathf.Clamp(steerInput, -1f, 1f);
        extThrottle = Mathf.Clamp01(throttleInput);

        // 최근 의도를 짧게 유지(프레임 누락 대비)
        extHold = ExtApplyWindow;
        // 매핑 원리:
        // - extSteer  :조향각 = baseTurnDeg * handling(응답) * 속도별 언더스티어 보정
        // - extThrottle :목표속도 = maxSpeed * throttle (회피 상황이면 cornerSlowFactor 적용)
        // - 가/감속은 accel/brakeDecel로 MoveTowards 처리 :차종별 성향 반영
    }
}
