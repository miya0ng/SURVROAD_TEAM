using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : LivingEntity, IDamagable
{

    [SerializeField]
    private EnemyManager enemyManager;
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
        enemyManager=GameObject.FindWithTag("EditorOnly").GetComponent<EnemyManager>();
        enemyPool=GameObject.FindWithTag("EditorOnly").GetComponent<EnemySpawner>();
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        if (enemyManager != null)
            enemyManager.Register(this);

        if (agent != null)
        {
            agent.isStopped = false;
        }
    }
    void OnDisable()
    {
        if (enemyManager != null)
            enemyManager.Unregister(this);
    }
    void OnDestroy()
    {

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
        base.OnDamage(damage, attacker);
        if (curHp == 0)
        {
            enemyPool.Return(this.gameObject);
            
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
        }
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {this.curHp}");
    }
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("충돌??");
        var player = collision.gameObject.GetComponent<PlayerBehaviour>();
        if (player != null)
        {
            Debug.Log("충돌!");
            player.OnDamage(collisionDamage, this);
        }
    }
}
