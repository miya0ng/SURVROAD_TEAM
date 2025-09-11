using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    public LayerMask targetLayer;
    private NavMeshAgent agent;
    private Transform target;
    private float traceDist = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        target = FindTarget(traceDist);

        if (target != null)
        {
            agent.isStopped = false;
            //Debug.Log("Target!");
            var success = agent.SetDestination(target.position);
            if(!success)
            {
                Debug.Log("????");
            }
        }
        else
        {
            Debug.Log("==== No Target! ===");
            agent.isStopped = true;
        }
        //Debug.Log(agent.isStopped + ", " + agent.speed);
        if (!agent.isOnNavMesh)
            Debug.LogError("에이전트가 NavMesh 위에 없음!");

        if (agent.hasPath)
            Debug.Log("경로 있음");
        else
            Debug.LogWarning("경로 없음");

        Debug.Log("남은 거리: " + agent.remainingDistance);
        Debug.Log(target.position);
    }
    protected void AttackPlayer()
    {
        //if (target == null || Vector3.Distance(transform.position, target.position) > AttackDist)
        //{
        //    return;
        //}

        //var lookPos = target.position;
        //lookPos.y = transform.position.y;
        //transform.LookAt(lookPos);

        // TODO: IDamagable 인터페이스 추가

    }

    protected Transform FindTarget(float radius)
    {
        var colliders = Physics.OverlapSphere(transform.position, radius, targetLayer);

        if (colliders.Length == 0)
        {
            return null;
        }

        var nearest = colliders
            .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
            .First();

        return nearest.transform;
    }
}
