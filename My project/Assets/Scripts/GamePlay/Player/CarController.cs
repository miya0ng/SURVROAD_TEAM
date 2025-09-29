using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;

    [SerializeField] private PlayerStatusEffects status;

    [Header("Speed/Accel")]
    [SerializeField] private float normalMaxSpeed = 35f;
    [SerializeField] private float boostedMaxSpeed = 60f;
    [SerializeField] private float maxForwardSpeed = 35f; // 현재 적용값
    [SerializeField] private float maxReverseSpeed = 10f;   // m/s
    [SerializeField] private float baseAccel = 12f;         // 가속 기본 가속도
    [SerializeField] private float baseBrake = 20f;         // 브레이크 기본 감속도
    [SerializeField] private float engineBrake = 3.5f;      // 엑셀 OFF 시 감속

    [Header("Booster State")]
    public bool IsBoosterOn { get; private set; }

    public event Action<bool> OnBoosterStateChanged;
    // 인스펙터에서 연결 (애니/사운드/아이콘 토글 등)
    [SerializeField] private UnityEvent onBoosterTurnedOn;
    [SerializeField] private UnityEvent onBoosterTurnedOff;
    [SerializeField] private GameObject boosterFx;   // 파티클 루트(차량 자식)
    [SerializeField] private AudioSource boosterLoop; // 루프 사운드(선택)

    [Header("Steer")]
    [SerializeField] private float turnRate = 140f;         // deg/sec (조향 기본값)
    [SerializeField] private float stability = 4.0f;        // 고속 안정화(슬립 각 되돌림)

    [Header("Grip & Drift")]
    [SerializeField] private float lateralDamp = 8f;        // 기본 횡감쇠(그립)
    [SerializeField] private float downforce = 5f;          // 속도 비례 다운포스
    [SerializeField] private float driftBlendSpeed = 6f;    // 드리프트 on/off 블렌딩 속도

    [Header("Curves")]
    public AnimationCurve accelCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
    public AnimationCurve brakeCurve = AnimationCurve.Linear(0, 0.6f, 1, 1);
    public AnimationCurve engineBrakeCurve = AnimationCurve.Linear(0, 0.2f, 1, 1);
    public AnimationCurve steerCurve = AnimationCurve.Linear(0, 1, 1, 0.25f);
    public AnimationCurve gripCurve = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve driftGripCurve = AnimationCurve.Linear(0, 0.45f, 1, 0.65f);
    public AnimationCurve driftYawBoostCurve = new AnimationCurve(
        new Keyframe(0f, 1.0f),
        new Keyframe(0.5f, 1.3f),
        new Keyframe(1f, 1.05f)
    );


    private bool isLeft;
    private bool isRight;
    private bool isAccel;
    private bool isBrake;
    private bool isDrift;

    float driftT;
    public Vector3 velLocal;
    public float hAxis = 1f;

    void Reset() { rb = GetComponent<Rigidbody>(); }

    void OnEnable()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (status)
            status.OnTurboActiveChanged += SetBooster;
    }

    void OnDisable()
    {
        if (status)
            status.OnTurboActiveChanged -= SetBooster;
    }

    public void SetBooster(bool on)
    {
        if (IsBoosterOn == on) return;
        IsBoosterOn = on;
        maxForwardSpeed = on ? boostedMaxSpeed : normalMaxSpeed;

        if (boosterFx) boosterFx.SetActive(on);
        if (boosterLoop) { boosterLoop.loop = true; if (on) boosterLoop.Play(); else boosterLoop.Stop(); }

        OnBoosterStateChanged?.Invoke(on);
        if (on) onBoosterTurnedOn?.Invoke();
        else onBoosterTurnedOff?.Invoke();
    }
    public void ButtonState(UiPlayButton.ButtonType button, bool isHeld)
    {
        switch (button)
        {
            case UiPlayButton.ButtonType.Left:
                isLeft = isHeld;
                break;
            case UiPlayButton.ButtonType.Right:
                isRight = isHeld;
                break;
            case UiPlayButton.ButtonType.Accel:
                isAccel = isHeld;
                break;
            case UiPlayButton.ButtonType.Break:
                isBrake = isHeld;
                break;
            case UiPlayButton.ButtonType.Drift:
                isDrift = isHeld;
                break;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 fwd = transform.forward;

        float turboMul = (status && status.TurboUnlimited) ? 1.4f : 1f; // 원하면 값 조절
        float accelNow = baseAccel * turboMul;
        float maxFwdNow = maxForwardSpeed * turboMul;

        Vector3 v = rb.linearVelocity;
        float fwdSpeed = Vector3.Dot(v, fwd);
        float speedAbs = Mathf.Abs(fwdSpeed);
        float norm = Mathf.InverseLerp(0f, maxFwdNow, speedAbs);

        bool accelEffective = IsBoosterOn || isAccel || Input.GetKey(KeyCode.W);
        float acc = 0f;

        if (isLeft || Input.GetKey(KeyCode.A)) hAxis = -1f;
        else if (isRight || Input.GetKey(KeyCode.D)) hAxis = 1f;
        else hAxis = 0f;


        if (accelEffective)
        {
            float aMul = accelCurve.Evaluate(norm);
            acc += accelNow * aMul;
        }
        else
        {
            if (!IsBoosterOn)
            {
                float ebMul = engineBrakeCurve.Evaluate(norm);
                acc -= engineBrake * ebMul * Mathf.Sign(fwdSpeed);
            }
        }

        if (isBrake || Input.GetKey(KeyCode.S))
        {
            float bMul = brakeCurve.Evaluate(norm);
            if (fwdSpeed > 0.5f) acc -= baseBrake * bMul;
            else acc -= baseAccel * 0.6f;
        }

        float targetMax = (fwdSpeed >= 0f) ? maxForwardSpeed : maxReverseSpeed;
        if (Mathf.Abs(fwdSpeed) >= targetMax && Mathf.Sign(acc) == Mathf.Sign(fwdSpeed))
            acc = 0f;

        rb.AddForce(fwd * acc, ForceMode.Acceleration);

        float targetDrift = isDrift ? 1f : 0f;
        driftT = Mathf.MoveTowards(driftT, targetDrift, driftBlendSpeed * dt);

        velLocal = transform.InverseTransformVector(rb.linearVelocity);
        float grip = Mathf.Lerp(gripCurve.Evaluate(norm), driftGripCurve.Evaluate(norm), driftT);
        velLocal.x = Mathf.Lerp(velLocal.x, 0f, grip * lateralDamp * dt);
        rb.linearVelocity = transform.TransformVector(velLocal);

        float steerMul = steerCurve.Evaluate(norm);
        float yawBoost = Mathf.Lerp(1f, driftYawBoostCurve.Evaluate(norm), driftT);
        float yawDeg = hAxis * turnRate * steerMul * yawBoost * dt;
        rb.MoveRotation(Quaternion.Euler(0f, yawDeg, 0f) * rb.rotation);

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 dir = rb.linearVelocity.normalized;
            float signed = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            rb.AddTorque(Vector3.up * -signed * stability * dt, ForceMode.Acceleration);
        }

        rb.AddForce(-transform.up * (downforce * norm), ForceMode.Acceleration);
    }
}