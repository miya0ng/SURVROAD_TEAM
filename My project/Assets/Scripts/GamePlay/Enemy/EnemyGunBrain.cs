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
    [SerializeField] private float preferMin = 14f;   // ��ȣ ��Ÿ� ����
    [SerializeField] private float preferMax = 24f;   // ��ȣ ��Ÿ� ����
    [SerializeField] private float shootRange = 30f;  // ��� �ִ� �Ÿ�

    [Header("Motion")]
    [SerializeField] private float orbitStrength = 0.55f; // ��� ���� ����ġ
    [SerializeField] private float strafeJitter = 0.5f;   // �¿� ����(�̼� �䵿)
    [SerializeField] private float aimThrottle = 0.65f;   // ���� �� ����Ʋ(�߰Ÿ����� ����)

    private EnemyCarController car;
    private Transform target;

    private float jitterSign = 1f;
    private float jitterT;

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
        // Ǯ���� Pop�� ���� ��ǥ/���� ����ȭ�� ���� 1������ ���
        armed = false;
        StartCoroutine(ArmNextFrame());
    }

    System.Collections.IEnumerator ArmNextFrame()
    {
        yield return null; // �� ������ ���
        armed = true;
    }

    void Update()
    {
        if (!armed || !target || !car) return;

        Vector3 pos = transform.position;
        Vector3 to = target.position - pos;
        to.y = 0f;

        float dist = to.magnitude;
        if (dist < 0.001f) return;

        // ===== ��� ����(��/��) ���� ��ȯ =====
        jitterT -= Time.deltaTime;
        if (jitterT <= 0f)
        {
            jitterT = Random.Range(0.7f, 1.6f);
            jitterSign = Random.value < 0.5f ? -1f : 1f;
        }

        // ===== ��Ÿ� ������ ���� ����/����Ʋ =====
        float throttle = 1f;
        float steer = 0f;

        // ��ǥ�� ���� ������ ���� ����(����)
        Vector3 right = Vector3.Cross(Vector3.up, to.normalized);
        Vector3 orbitDir = right * orbitStrength * jitterSign;

        // A*�� �߰� ���̶� �����ϰ�, ���⼱ �̼� ���⸸ �����ش�.
        Vector3 desiredDir = (to.normalized + orbitDir).normalized;
        float orbitSteer = Vector3.SignedAngle(transform.forward, desiredDir, Vector3.up) / 45f;
        steer = Mathf.Clamp(orbitSteer + (strafeJitter * jitterSign * 0.15f), -1f, 1f);

        // ��ȣ ��Ÿ��뿡 ���� ����Ʋ ����
        if (dist < preferMin) throttle = 0.5f;   // ��¦ ���� ����(ȸ�� + ����)
        else if (dist > preferMax) throttle = 1.0f;   // �� �� ����
        else throttle = aimThrottle;

        car.SetDesired(steer, throttle);

        // ===== ��� =====
        if (gun && dist <= shootRange)
        {
            // LOS: �����ϸ� ��(=�ѱ� �ڽ�)�� ��ġ ����
            Vector3 origin = gun.transform.position;
            Vector3 dest = target.position + Vector3.up * 0.6f;

            bool blocked = Physics.Linecast(origin, dest, losMask, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                // ��Ÿ��/�߻� Ÿ�̹��� Gun�� ���� �� Brain�� ���ø�
                gun.TickAutoFireToward(target.position);
            }
        }
    }
}
