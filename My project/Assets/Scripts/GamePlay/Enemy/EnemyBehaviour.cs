using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static Bullet;
public class EnemyBehaviour : LivingEntity, IDamagable
{
    private ItemManager itemManager;
    private EnemySpawner enemyPool;

    public LayerMask targetLayer;
    private NavMeshAgent agent;
    private Transform target;
    private float traceDist = 100f;
    private float collisionDamage = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        //enemyManager = GetComponent<EnemyManager>();
        itemManager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        enemyPool = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemySpawner>();
        agent = GetComponent<NavMeshAgent>();
        maxHp = 50;
    }
    void Start()
    {
        teamId = TeamId.Enemy;
    }
    void OnEnable()
    {
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }
    void OnDisable()
    {
        if (agent != null)
        {
            //agent.isStopped = true;
        }
    }
    // Update is called once per frame
    void Update()
    {
        target = FindTarget(traceDist);

        if (target != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            var success = agent.SetDestination(target.position);
        }
        else if(target == null && agent.isOnNavMesh)
        {
            //Debug.Log("==== No Target! ===");
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

        // TODO: 적 무기 추가

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
        base.OnDamage(damage, attacker);

        var flash = GetComponentInChildren<HitFlash>();
        if (flash != null)
            flash.PlayFlash();
    }

    protected override void Die()
    {
        base.Die();
        itemManager.DropFromEnemy(transform.position);

        if (enemyPool != null)
        {
            enemyPool.Return(gameObject);
            enemyPool.ActiveEnemyCount--;
        }

        //else
        //{
        //    Destroy(gameObject);
        //}
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
