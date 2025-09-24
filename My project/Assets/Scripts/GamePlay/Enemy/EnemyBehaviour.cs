using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : LivingEntity
{
    private ItemManager itemManager;

    public LayerMask targetLayer;
    private NavMeshAgent agent;
    private Transform target;
    private float traceDist = 9999f;
    private float collisionDamage = 10f;

    protected override void Awake()
    {
        base.Awake();
        itemManager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        agent = GetComponent<NavMeshAgent>();
        maxHp = 18;
        curHp = maxHp;
    }

    void OnEnable()
    {
        if (agent && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    void OnDisable()
    {
        // 필요 시 정지/상태 초기화 로직
        // if (agent) agent.isStopped = true;
    }

    void Update()
    {
        target = FindTarget(traceDist);

        if (agent && agent.isOnNavMesh)
        {
            if (target != null)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
            else
            {
                agent.isStopped = true;
            }
        }
    }

    protected Transform FindTarget(float radius)
    {
        var colliders = Physics.OverlapSphere(transform.position, radius, targetLayer);
        if (colliders.Length == 0) return null;

        var nearest = colliders
            .OrderBy(x => Vector3.Distance(x.transform.position, transform.position))
            .First();

        return nearest.transform;
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);

        var flash = GetComponentInChildren<HitFlash>();
        if (flash != null)
            flash.PlayFlash();
    }

    protected override void Die(LivingEntity killer = null)
    {
        base.Die();

        itemManager.DropFromEnemy(transform.position);
        // if (enemyPool != null) enemyPool.Return(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<PlayerBehaviour>();
        if (player != null)
        {
            player.OnDamage(collisionDamage, this);
        }
    }
}