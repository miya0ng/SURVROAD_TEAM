using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : LivingEntity, IDamagable
{
    public LayerMask targetLayer;
    private NavMeshAgent agent;
    private Transform target;
    private float traceDist = 100f;
    private float collisionDamage = 10f;

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
            var success = agent.SetDestination(target.position);
        }
        else
        {
            Debug.Log("==== No Target! ===");
            agent.isStopped = true;
        }
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

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        this.curHp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {this.curHp}");
    }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("충돌??");
        var player = collision.gameObject.GetComponent<PlayerBehaviour>();
        if (player != null )
        {
            Debug.Log("충돌!");
            player.OnDamage(collisionDamage, this);
        }
    }
}
