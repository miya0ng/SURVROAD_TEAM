using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform emptySpawnPoint;
    [SerializeField] private GameObject enemy;
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 300f;
    [SerializeField] private int makePoolCount = 200;
    [SerializeField] private int enemyCoSpawnCount = 1;
    [SerializeField] private float spawnInterval = 1f;

    public bool IsWaveCleared => curSpawnCount >= waveSpawnCount && ActiveEnemyCount <= 0;
    public event System.Action OnWaveCleared;

    private readonly Queue<GameObject> EnemyPool = new Queue<GameObject>();
    private readonly List<LivingEntity> enemies = new List<LivingEntity>();
    public List<LivingEntity> GetEnemies() => enemies;

    public int curSpawnCount { get; private set; }
    public int waveSpawnCount { get; set; }
    public int ActiveEnemyCount { get; private set; }
    public Coroutine coroutine { get; private set; }

    // onDeath 안전 구독/해제용 매핑
    private readonly Dictionary<LivingEntity, GameObject> deathMap = new();

    public void Register(LivingEntity e)
    {
        if (e && !enemies.Contains(e)) enemies.Add(e);
    }
    public void Unregister(LivingEntity e)
    {
        if (e) enemies.Remove(e);
    }

    void Awake()
    {
        if (mainCam == null) Debug.LogWarning("EnemySpawner: mainCam 미할당");
        MakePool();
    }

    void Start()
    {
        StartSpawner();
    }

    public void StartSpawner()
    {
        if (coroutine != null) StopSpawner();
        coroutine = StartCoroutine(SpawnEnemy());
    }

    public void StopSpawner()
    {
        if (coroutine != null)
        {
            curSpawnCount = 0;
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    private IEnumerator SpawnEnemy()
    {
        while (curSpawnCount < waveSpawnCount)
        {
            SpawnOutsideView();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void MakePool()
    {
        for (int i = 0; i < makePoolCount; i++)
        {
            var e = Instantiate(enemy);
            var agent = e.GetComponent<NavMeshAgent>();
            if (agent) agent.enabled = false;
            e.transform.SetParent(emptySpawnPoint, false);
            e.gameObject.SetActive(false);
            EnemyPool.Enqueue(e);
        }
    }

    // 화면 밖, NavMesh 위에 즉시 한 마리 스폰
    public void SpawnOneImmediate()
    {
        if (player == null) return;
        if (!TryFindSpawnPosition(out var pos)) return;

        var enemyObj = Get();
        if (enemyObj == null) return;

        ActivateEnemy(enemyObj, pos);
        curSpawnCount++;
        ActiveEnemyCount++;
    }

    // 배치 루프(여러 마리)
    public void SpawnOutsideView()
    {
        for (int i = 0; i < enemyCoSpawnCount; i++)
        {
            if (!TryFindSpawnPosition(out var spawnPos)) continue;

            var enemyObj = Get();
            if (enemyObj == null) continue;

            ActivateEnemy(enemyObj, spawnPos);
            curSpawnCount++;
            ActiveEnemyCount++;
        }
    }

    // 화면 밖 + NavMesh 위치 찾기
    private bool TryFindSpawnPosition(out Vector3 spawnPos)
    {
        for (int safety = 0; safety < 50; safety++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = player.position + dir * spawnRadius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 50f, NavMesh.AllAreas))
            {
                if (!IsOnScreen(mainCam, hit.position))
                {
                    spawnPos = hit.position;
                    return true;
                }
            }
        }
        spawnPos = Vector3.zero;
        return false;
    }

    private bool IsOnScreen(Camera cam, Vector3 worldPos)
    {
        if (cam == null) return false;
        Vector3 v = cam.WorldToViewportPoint(worldPos);
        return (v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f);
    }

    // 풀에서 하나 꺼내기
    public GameObject Get()
    {
        if (EnemyPool.Count <= 0) MakePool();
        if (EnemyPool.Count == 0) return null;

        var e = EnemyPool.Dequeue();
        var le = e.GetComponent<LivingEntity>();
        if (le) Register(le);
        return e;
    }

    // 풀로 반납
    public void Return(GameObject e)
    {
        if (!e) return;

        var le = e.GetComponent<LivingEntity>();
        if (le)
        {
            UnhookDeath(le);
            Unregister(le);
        }

        e.SetActive(false);
        e.transform.SetParent(emptySpawnPoint, false);
        EnemyPool.Enqueue(e);

        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
        CheckWaveCleared();
    }
    private void CheckWaveCleared()
    {
        if (IsWaveCleared) OnWaveCleared?.Invoke();
    }

    private void ActivateEnemy(GameObject enemyObj, Vector3 pos)
    {
        if (!NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas))
        {
            Return(enemyObj); 
            return;
        }

        enemyObj.transform.SetPositionAndRotation(
            pos,
            Quaternion.LookRotation((player.position - pos).normalized, Vector3.up)
        );

        var agent = enemyObj.GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;
            agent.Warp(hit.position);
            agent.isStopped = false;
        }

        if (agent.isOnNavMesh)
            agent.isStopped = false;

        enemyObj.SetActive(true);

        var le = enemyObj.GetComponent<LivingEntity>();
        if (le)
        {
            Register(le);
            HookDeath(le, enemyObj);
        }
    }

    private void HookDeath(LivingEntity le, GameObject go)
    {
        UnhookDeath(le);
        deathMap[le] = go;
        le.onDeath += OnEnemyDeath;
    }

    private void UnhookDeath(LivingEntity le)
    {
        if (le == null) return;
        le.onDeath -= OnEnemyDeath;
        deathMap.Remove(le);
    }

    private void OnEnemyDeath(LivingEntity dead)
    {
        if (dead == null) return;
        if (deathMap.TryGetValue(dead, out var go))
        {
            Return(go);
        }
        else
        {
            foreach (var kv in deathMap)
            {
                if (!kv.Key || !kv.Key.gameObject.activeSelf)
                {
                    Return(kv.Value);
                    break;
                }
            }
        }
    }

    public void DebugSpawn(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnOneImmediate();
    }
}
