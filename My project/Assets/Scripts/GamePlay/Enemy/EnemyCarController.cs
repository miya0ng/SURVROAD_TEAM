using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCarController : MonoBehaviour, IExternalSpeedScale
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

    [Header("Crowd (Anti-bumping)")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float neighborRadius = 6f;
    [SerializeField] private int maxNeighbors = 16;
    [SerializeField] private float separationWeight = 1.25f;
    [SerializeField] private float sepFrontBias = 0.2f;
    [SerializeField] private int queryEveryN = 3;
    [SerializeField] private bool softDepenetrate = true;

    private Rigidbody rb;
    private Transform target;

    // === 외부 입력 버퍼 ===
    float extSteer;
    float extThrottle;
    float extHold = 0f;
    const float ExtApplyWindow = 0.15f;

    // === Crowd 계산 버퍼 ===
    static readonly Collider[] _neighBuf = new Collider[64];
    int _frameSeed;

    // === 🆕 외부 감속 시스템 (전기 트랩용) ===
    private float externalSpeedScale = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezePositionY | 
                         RigidbodyConstraints.FreezeRotationZ;

        _frameSeed = Random.Range(0, Mathf.Max(1, queryEveryN));
    }

    public void Bind(Transform target)
    {
        this.target = target;
    }

    public void SetExternalSpeedScale(float scale)
    {
        externalSpeedScale = Mathf.Clamp(scale, 0.05f, 1f);
        
        // 디버그 로그 (테스트 후 제거 가능)
        if (scale < 0.99f)
        {
            Debug.Log($"[{gameObject.name}] 감속 적용: {(1f - scale) * 100:F0}% (scale={scale:F2})");
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 pos = rb.position;
        Vector3 forward = transform.forward;

        float effectiveMaxSpeed = maxSpeed * externalSpeedScale;
        float effectiveAccel = accel * externalSpeedScale;

        // ── 공통: 센서(회피/브레이크 판정) 먼저 계산 ──
        bool needBrake = false;
        Vector3 avoidDir = Vector3.zero;
        if (RayHit(forward, feelerLen, out var hitC)) 
        { 
            avoidDir += Vector3.Reflect(forward, hitC.normal); 
            needBrake = true; 
        }
        Vector3 leftDir = Quaternion.Euler(0f, -feelerSideAngle, 0f) * forward;
        if (RayHit(leftDir, sideFeelerLen, out var hitL)) 
        { 
            avoidDir += Vector3.Reflect(leftDir, hitL.normal); 
            needBrake = true; 
        }
        Vector3 rightDir = Quaternion.Euler(0f, feelerSideAngle, 0f) * forward;
        if (RayHit(rightDir, sideFeelerLen, out var hitR)) 
        { 
            avoidDir += Vector3.Reflect(rightDir, hitR.normal); 
            needBrake = true; 
        }

        float speed = rb.linearVelocity.magnitude;

        // ── 1) 외부 입력 우선 적용 ──
        if (extHold > 0f)
        {
            extHold -= dt;

            float turnLimit = Mathf.Lerp(baseTurnDeg, baseTurnDeg * 0.33f, 
                Mathf.InverseLerp(0f, effectiveMaxSpeed, speed)); // 🆕 감속 반영

            float turnDeg = Mathf.Clamp(extSteer, -1f, 1f) * turnLimit;

            Quaternion q = Quaternion.AngleAxis(turnDeg * handling * dt, Vector3.up);
            rb.MoveRotation(rb.rotation * q);

            float targetSpeed = Mathf.Lerp(0f, effectiveMaxSpeed, Mathf.Clamp01(extThrottle)); // 🆕 감속 반영
            if (needBrake) targetSpeed *= cornerSlowFactor;

            float dv = targetSpeed - speed;
            float a = dv >= 0f ? effectiveAccel : brakeDecel; // 🆕 감속 반영
            float newSpeed = Mathf.MoveTowards(speed, targetSpeed, a * dt);

            Vector3 vel = transform.forward * newSpeed;
            rb.linearVelocity = new Vector3(vel.x, rb.linearVelocity.y, vel.z);

            if (visualRoot)
            {
                Vector3 face = rb.linearVelocity.sqrMagnitude > 0.1f ? rb.linearVelocity : transform.forward;
                visualRoot.forward = Vector3.Slerp(visualRoot.forward, face, 0.2f);
            }

            if (softDepenetrate) SoftDepenetrate();
            return;
        }

        // ── 2) 내부 추격/회피 ──
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
                desired += right * Mathf.Sign(Vector3.SignedAngle(forward, desired, Vector3.up))
                           * 0.35f * (1f - desired.magnitude / (desiredRadius * 1.2f));
            }
        }

        Vector3 pursueDir = desired.sqrMagnitude > 0.000001f ? desired.normalized : forward;

        // ── 군집 분리(Separation) ──
        int neighs;
        Vector3 sepDir = ComputeSeparation(pos, forward, out neighs);

        // ── 스티어 합성 ──
        Vector3 steerDir = (avoidDir != Vector3.zero)
            ? (pursueDir * pursueWeight + avoidDir.normalized * avoidWeight + sepDir * separationWeight).normalized
            : (pursueDir * pursueWeight + sepDir * separationWeight).normalized;

        float turnLimit2 = Mathf.Lerp(baseTurnDeg, baseTurnDeg * 0.33f, 
            Mathf.InverseLerp(0f, effectiveMaxSpeed, speed)); // 🆕 감속 반영
        float turn2 = Mathf.Clamp(Vector3.SignedAngle(forward, steerDir, Vector3.up), -turnLimit2, turnLimit2);

        Quaternion q2 = Quaternion.AngleAxis(turn2 * handling * dt, Vector3.up);
        rb.MoveRotation(rb.rotation * q2);

        float targetSpeed2 = effectiveMaxSpeed; // 🆕 감속 반영
        if (needBrake) targetSpeed2 *= cornerSlowFactor;

        // 밀집 시 꼬리물기 방지용 소감속
        if (neighs >= 3) targetSpeed2 *= 0.85f;

        float dv2 = targetSpeed2 - speed;
        float a2 = dv2 >= 0f ? effectiveAccel : brakeDecel; // 🆕 감속 반영
        float newSpeed2 = Mathf.MoveTowards(speed, targetSpeed2, a2 * dt);

        Vector3 vel2 = transform.forward * newSpeed2;
        rb.linearVelocity = new Vector3(vel2.x, rb.linearVelocity.y, vel2.z);

        if (visualRoot)
        {
            Vector3 face = rb.linearVelocity.sqrMagnitude > 0.1f ? rb.linearVelocity : transform.forward;
            visualRoot.forward = Vector3.Slerp(visualRoot.forward, face, 0.2f);
        }

        if (softDepenetrate) SoftDepenetrate();
    }

    bool RayHit(Vector3 dir, float len, out RaycastHit hit)
    {
        return Physics.Raycast(rb.position + Vector3.up * 0.5f, dir, out hit, len, 
            obstacleMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// 외부에서 주는 "의도"를 차량 스펙에 맞게 즉시 적용
    /// </summary>
    public void SetDesired(float steerInput, float throttleInput)
    {
        extSteer = Mathf.Clamp(steerInput, -1f, 1f);
        extThrottle = Mathf.Clamp01(throttleInput);
        extHold = ExtApplyWindow;
    }

    // ─────────────────────────────────────────────────────────────
    // 군집 분리: NonAlloc + 타임슬라이스
    // ─────────────────────────────────────────────────────────────
    Vector3 ComputeSeparation(Vector3 pos, Vector3 fwd, out int hitCount)
    {
        hitCount = 0;
        if (queryEveryN <= 1 ? false : ((Time.frameCount + _frameSeed) % queryEveryN != 0))
            return Vector3.zero;

        int n = Physics.OverlapSphereNonAlloc(
            pos, neighborRadius, _neighBuf, enemyLayer, QueryTriggerInteraction.Ignore);

        Vector3 accum = Vector3.zero;
        int used = 0;
        for (int i = 0; i < n && used < maxNeighbors; i++)
        {
            var c = _neighBuf[i];
            if (c == null) continue;
            var other = c.attachedRigidbody;
            if (other == null || other == rb) continue;

            Vector3 toMe = (pos - other.position);
            toMe.y = 0f;
            float d2 = toMe.sqrMagnitude;
            if (d2 < 0.0001f) continue;

            // 전방 반구 가중치
            float front = Mathf.Max(0f, Vector3.Dot(fwd, (-toMe).normalized));
            float w = (1.0f / Mathf.Max(0.5f, d2)) * Mathf.Lerp(sepFrontBias, 1f, front);

            accum += toMe.normalized * w;
            used++;
        }

        hitCount = used;
        return (accum == Vector3.zero) ? Vector3.zero : accum.normalized;
    }

    // ─────────────────────────────────────────────────────────────
    // 겹침 최소 보정
    // ─────────────────────────────────────────────────────────────
    void SoftDepenetrate()
    {
        var col = GetComponent<Collider>();
        if (!col) return;

        int n = Physics.OverlapSphereNonAlloc(
            rb.position, neighborRadius * 0.6f, _neighBuf, enemyLayer, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            var otherCol = _neighBuf[i];
            if (otherCol == null) continue;
            var otherRb = otherCol.attachedRigidbody;
            if (otherRb == null || otherRb == rb) continue;

            if (Physics.ComputePenetration(
                col, transform.position, transform.rotation,
                otherCol, otherCol.transform.position, otherCol.transform.rotation,
                out Vector3 dir, out float dist))
            {
                float push = Mathf.Min(dist, 0.02f);
                if (push > 0f)
                    rb.MovePosition(rb.position + dir * push);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 군집 반경 가시화
        Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.2f);
        Gizmos.DrawWireSphere(Application.isPlaying ? rb.position : transform.position, neighborRadius);

        // 센서 가시화
        Vector3 pos = Application.isPlaying ? rb.position : transform.position;
        Vector3 fwd = transform.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(pos + Vector3.up * 0.5f, fwd * feelerLen);
        Vector3 leftDir = Quaternion.Euler(0f, -feelerSideAngle, 0f) * fwd;
        Vector3 rightDir = Quaternion.Euler(0f, feelerSideAngle, 0f) * fwd;
        Gizmos.DrawRay(pos + Vector3.up * 0.5f, leftDir * sideFeelerLen);
        Gizmos.DrawRay(pos + Vector3.up * 0.5f, rightDir * sideFeelerLen);
        
        // 감속 상태 표시
        if (Application.isPlaying && externalSpeedScale < 0.99f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos + Vector3.up * 2f, 0.5f);
        }
    }
#endif
}