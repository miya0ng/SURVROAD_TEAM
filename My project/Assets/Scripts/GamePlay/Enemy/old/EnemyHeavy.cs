// Assets/Scripts/Enemy/Behaviours/EnemyHeavy.cs
using UnityEngine;

public class EnemyHeavy : EnemyBehaviourBase
{
    private float stuckT;

    protected override void TickMove(float dt)
    {
        if (target && agent) agent.SetDestination(target.position);

        if (agent && agent.velocity.sqrMagnitude < 0.04f) stuckT += dt; else stuckT = 0f;
        if (stuckT > 0.5f && agent)
        {
            agent.speed = Mathf.Min(agent.speed * 1.05f, agent.speed + 0.5f);
        }
    }

    // 충돌 데미지는 EnemyBehaviourBase.OnCollisionEnter에서 공통 처리
}