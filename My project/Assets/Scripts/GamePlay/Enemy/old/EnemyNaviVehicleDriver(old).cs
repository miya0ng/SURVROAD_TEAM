// Assets/Scripts/Enemy/Movement/EnemyNavVehicleDriver.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(NavMeshAgent))]
public class EnemyNavVehicleDriver : MonoBehaviour
{
    [Header("Speed / Accel")]
    public float maxSpeed = 22f;          // RB 최고속도(물리)
    public float accelForce = 28f;        // 전진 가속 힘
    public float brakeForce = 36f;        // 정면 각도 크면 감속
    public float lateralFriction = 6f;    // 측미끄럼 억제

    [Header("Steering")]
    public float steerPower = 6f;         // 조향 응답(토크)
    public float steerAssist = 0.15f;     // 고속 직진 안정 보조(요억제)
    public float steerAngleForFull = 45f; // ±45° 이상이면 최대 조향

    [Header("Downforce")]
    public float downforce = 10f;         // 속도에 비례한 다운포스

    [Header("Agent Sync")]
    public float agentRepathRate = 0.2f;  // 경로 갱신 주기(초)

    Rigidbody rb;
    NavMeshAgent agent;
    float repathTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

        // Agent는 "경로만" 계산
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.autoRepath = true; //재탐색

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void FixedUpdate()
    {
        if (!agent || !agent.isOnNavMesh) return;

        Vector3 desiredVel = agent.desiredVelocity;          // 경로따라 가고싶은 속도 벡터
        if (desiredVel.sqrMagnitude < 0.0001f) desiredVel = transform.forward * 0.01f;

        Vector3 fwd = transform.forward;
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        // === 1) 조향 ===
        float ang = Vector3.SignedAngle(fwd, desiredVel.normalized, Vector3.up);       // -180~180
        float steer = Mathf.Clamp(ang / Mathf.Max(1f, steerAngleForFull), -1f, 1f);    // -1~1
        rb.AddTorque(Vector3.up * steer * steerPower, ForceMode.Acceleration);

        // 고속 안정화(요 억제): 현재 속도 방향과 기체 앞방향이 어긋나면 살짝 보정
        if (speed > 0.1f)
        {
            Vector3 velDir = vel.normalized;
            float yawOff = Vector3.SignedAngle(fwd, velDir, Vector3.up);
            rb.AddTorque(Vector3.up * -yawOff * steerAssist * 0.02f, ForceMode.VelocityChange);
        }

        // === 2) 추진/감속 ===
        // 경로가 급히 꺾이면(ang 큼) 브레이크 쪽 비중
        float turnBrake = Mathf.InverseLerp(10f, 60f, Mathf.Abs(ang)); // 10~60도 사이에서 0→1
        float throttle = 1f - turnBrake;                               // 코너에서 액셀 살짝 덜 밟기

        // 목표 속도 크기를 Agent.speed 기준으로 정규화
        float wantSpeed01 = Mathf.Clamp01(desiredVel.magnitude / Mathf.Max(0.1f, agent.speed));
        float drive = throttle * wantSpeed01;

        // 전진 가속
        rb.AddForce(fwd * (drive * accelForce), ForceMode.Acceleration);

        // 선회 브레이크(측면 슬립 억제 + 감속)
        if (turnBrake > 0.05f)
            rb.AddForce(-vel * (turnBrake * brakeForce * 0.05f), ForceMode.Acceleration);

        // === 3) 측미끄럼 억제(드리프트 완화) ===
        Vector3 lateral = Vector3.ProjectOnPlane(vel, Vector3.up) - fwd * Vector3.Dot(vel, fwd);
        rb.AddForce(-lateral * lateralFriction, ForceMode.Acceleration);

        // === 4) 다운포스 ===
        rb.AddForce(-Vector3.up * (speed * downforce * 0.02f), ForceMode.Force);

        // === 5) 최고속도 제한 ===
        if (speed > maxSpeed)
            rb.linearVelocity = vel.normalized * maxSpeed;

        // === 6) Agent 위치 동기화 ===
        agent.nextPosition = rb.position;

        // === 7) 주기적 Repath(선택) ===
        repathTimer += Time.fixedDeltaTime;
        if (repathTimer >= agentRepathRate)
        {
            repathTimer = 0f;
            if (agent.hasPath && agent.remainingDistance < 0.5f)
                agent.ResetPath(); // 거의 도착이면 경로 비움
        }
    }
}
