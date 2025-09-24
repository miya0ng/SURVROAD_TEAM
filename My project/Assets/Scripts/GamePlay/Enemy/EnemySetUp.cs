// Assets/Scripts/Enemy/EnemySetup.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemySetup : MonoBehaviour
{
    public void Apply(EnemyCarDataWrapper wrapper)
    {
        if (wrapper == null) return;

        var agent = GetComponent<NavMeshAgent>();
        var rb = GetComponent<Rigidbody>();
        wrapper.ApplyTo(agent, rb, agentAsKinematic: false); // Agent 중심이면 false

        // 행동 기본값 튜닝 + CSV 전투 파라미터 주입
        var baseComp = GetComponent<EnemyBehaviourBase>();
        if (baseComp != null)
        {
            switch (wrapper.MoveStyle)
            {
                case EnemyMoveStyle.Shooter:
                    if (baseComp is EnemyShooter sh)
                    {
                        sh.desiredRadius = 18f;
                        sh.orbitAngular = 35f;
                        sh.strafeSpeed = 0.6f;
                    }
                    break;

                case EnemyMoveStyle.Suicide:
                    if (baseComp is EnemySuicide su)
                    {
                        su.zigzagAmp = 3f;
                        su.zigzagFreq = 1.2f;
                        su.igniteDistance = 8f;
                        su.burstMult = 1.4f;
                    }
                    break;

                case EnemyMoveStyle.Heavy:
                    if (baseComp is EnemyHeavy hv)
                    {
                        hv.plowForce = 25f;
                    }
                    break;

                case EnemyMoveStyle.Rush:
                default:
                    if (baseComp is EnemyRush rs)
                    {
                        rs.ramDistance = 6f;
                        rs.ramAccel = 18f;
                    }
                    break;
            }

            baseComp.InitFromData(wrapper); // ★ CSV 전투 파라미터(Attack*, Collision) 주입
        }
    }
}
