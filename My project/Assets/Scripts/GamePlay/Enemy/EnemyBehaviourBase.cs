// Assets/Scripts/Enemy/Behaviours/EnemyBehaviourBase.cs
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBehaviourBase : LivingEntity
{
    [Header("Common")]
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected float traceDist = 9999f;

    protected ItemManager itemManager;
    protected NavMeshAgent agent;
    [SerializeField] protected Transform target;

    [Header("Common Move")]
    public float desiredRadius = 12f;
    public float zigzagAmp = 0f;     // Suicide용
    public float zigzagFreq = 0f;    // Suicide용
    public float plowForce = 0f;     // Heavy용

    // CSV 전투 파라미터
    protected int attackDamage;
    protected float attackInterval;    // 초
    protected int collisionDamage;

    protected override void Awake()
    {
        base.Awake();
        itemManager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        agent = GetComponent<NavMeshAgent>();
        maxHp = 18;
        curHp = maxHp;
    }

    public virtual void InitFromData(EnemyCarDataWrapper wrap)
    {
        attackDamage = wrap.Row.AttackDamage;
        attackInterval = Mathf.Max(0.05f, wrap.Row.AttackInterval);
        collisionDamage = wrap.Row.CollisionDamage;
    }

    protected virtual void OnEnable()
    {
        if (agent && agent.isOnNavMesh) agent.isStopped = false;
    }

    protected virtual void OnDisable()
    {
        // if (agent) agent.isStopped = true;
    }

    protected virtual void Update()
    {
        target = FindTarget(traceDist);
        if (agent && agent.isOnNavMesh)
        {
            if (target != null)
            {
                agent.isStopped = false;
                TickMove(Time.deltaTime);
            }
            else
            {
                agent.isStopped = true;
            }
        }
    }

    protected Transform FindTarget(float radius)
    {
        var cols = Physics.OverlapSphere(transform.position, radius, targetLayer);
        if (cols.Length == 0) return null;
        return cols.OrderBy(c => Vector3.Distance(c.transform.position, transform.position)).First().transform;
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        var flash = GetComponentInChildren<HitFlash>();
        if (flash != null) flash.PlayFlash();
    }

    protected override void Die(LivingEntity killer = null)
    {
        base.Die();
        itemManager.DropFromEnemy(transform.position);
        // 풀링 반환 지점 (EnemySpawner가 onDeath로 Return 처리)
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<PlayerBehaviour>();
        if (player != null)
        {
            player.OnDamage(collisionDamage, this);
        }
    }

    protected abstract void TickMove(float dt);
}
