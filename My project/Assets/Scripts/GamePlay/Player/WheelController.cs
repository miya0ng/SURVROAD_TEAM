using UnityEngine;

public class WheelController : MonoBehaviour
{
    public enum Axle { Front, Rear }
    [Header("Setup")]
    [SerializeField] Axle axle = Axle.Front;
    [SerializeField] float wheelRadius = 0.32f;
    [SerializeField] float maxSteerAngle = 28f;
    [SerializeField] float steerLerp = 12f;
    [SerializeField] Transform visual = null;

    Rigidbody rb;
    Quaternion baseLocalRot;
    float steerAngle;
    float spinAngle;

    float steerInput;
    public void SetSteer(float steer) => steerInput = Mathf.Clamp(steer, -1f, 1f); 

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
        if (!visual) visual = transform;
        baseLocalRot = visual.localRotation;
    }

    void Update()
    {
        if (!rb) return;

        float targetSteer = (axle == Axle.Front) ? steerInput * maxSteerAngle : 0f;
        steerAngle = Mathf.Lerp(steerAngle, targetSteer, 1f - Mathf.Exp(-steerLerp * Time.deltaTime));


        float forwardSpeed = Vector3.Dot(rb.linearVelocity, rb.transform.forward); // m/s

        float omegaDegPerSec = (wheelRadius > 0.0001f) ? (forwardSpeed / wheelRadius) * Mathf.Rad2Deg : 0f;
        spinAngle += omegaDegPerSec * Time.deltaTime;

        Quaternion steerRot = Quaternion.Euler(0f, steerAngle, 0f);
        Quaternion spinRot = Quaternion.Euler(spinAngle, 0f, 0f);
        visual.localRotation = steerRot * spinRot;
    }
}
