using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemySuicide : EnemyBehaviourBase
{
    [Header("Suicide")]
    public float igniteDistance = 8f;  // 점화(자폭) 시작 거리
    public float burstMult = 1.4f;     // 점화 시 이동 속도 배수
    public float postIgniteDelay = 0f; // 필요 시 점화 후 지연

    private float t;
    private bool ignited;

    protected override void OnEnable()
    {
        base.OnEnable();
        t = 0f;
        ignited = false;

        // 점화 전에는 살짝 느리게(점화 시 배속)
        if (agent)
        {
            // B안: agent는 경로만 계산하지만 speed는 desiredVelocity 스케일에 영향
            agent.speed = Mathf.Max(1f, agent.speed / Mathf.Max(0.0001f, burstMult));
        }

        if (agent && agent.isOnNavMesh) agent.isStopped = false;
    }

    protected override void TickMove(float dt)
    {
        if (!target || !agent || !agent.isOnNavMesh) return;
        t += dt;

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.0001f) return;

        // 지그재그 목표점
        Vector3 right = Vector3.Cross(Vector3.up, to.normalized);
        Vector3 offset = right * Mathf.Sin(t * Mathf.Max(0.01f, zigzagFreq)) * zigzagAmp;
        Vector3 aim = target.position + offset;

        agent.SetDestination(aim);

        // 점화 트리거
        if (!ignited && dist < igniteDistance)
        {
            ignited = true;
            agent.speed *= burstMult;

            // 즉시 폭발
            var exploder = GetComponent<Exploder>();
            if (exploder)
            {
                // 공격력은 CSV에서 InitFromData로 들어온 attackDamage 사용
                // attacker는 자신으로 넘김
                exploder.Trigger(attackDamage, attacker: transform);
            }
            else
            {
                // Exploder 없음: 최소한 자기 제거
                GetComponent<LivingEntity>()?.OnDamage(999999f, this);
            }
        }
    }
}