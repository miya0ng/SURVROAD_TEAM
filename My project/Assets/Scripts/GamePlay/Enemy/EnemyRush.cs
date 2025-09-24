using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyRush : EnemyBehaviourBase
{
    [Header("Rush")]
    public float ramDistance = 6f;  // 돌진 시작 거리
    public float ramAccel = 18f;    // (참고) RB 드라이버에서 가감속을 쓰므로 여기서는 목표점만
    public float ramSpeedBoost = 1.15f;
    private float ramCd;

    protected override void OnEnable()
    {
        base.OnEnable();
        ramCd = 0f;
        if (agent && agent.isOnNavMesh) agent.isStopped = false;
    }

    protected override void TickMove(float dt)
    {
        if (!target || !agent || !agent.isOnNavMesh) return;

        Vector3 aim = target.position;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist < ramDistance && ramCd <= 0f)
        {
            // 살짝 앞으로 스루하는 느낌(추월 지점)
            aim = target.position + target.forward * 5f;

            // 속도 부스트: Agent가 경로 속도 상한을 제시하므로 약간 상향
            agent.speed *= ramSpeedBoost;

            ramCd = 1.2f; // 쿨다운
        }
        ramCd -= dt;

        agent.SetDestination(aim);
    }
}