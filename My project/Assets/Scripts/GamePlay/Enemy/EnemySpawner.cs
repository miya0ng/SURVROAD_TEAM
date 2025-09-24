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
    /*[SerializeField]*/ private float spawnRadius = 100f;
    /*[SerializeField]*/ private int makePoolCount = 100;
    /*[SerializeField]*/ private int enemyCoSpawnCount = 1;
    /*[SerializeField]*/ private float spawnInterval = 1f;

    public bool IsWaveCleared => curSpawnCount >= waveSpawnCount && ActiveEnemyCount <= 0;
    public event System.Action OnWaveCleared;

    private readonly Queue<GameObject> poolQ = new();
    private readonly HashSet<GameObject> poolSet = new();

    private readonly List<LivingEntity> enemies = new();
    public List<LivingEntity> GetEnemies() => enemies;

    public int curSpawnCount { get; private set; }
    public int waveSpawnCount { get; set; }
    public int ActiveEnemyCount { get; private set; }
    public Coroutine coroutine { get; private set; }

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
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (mainCam == null) mainCam = Camera.main;

        if (player == null) Debug.LogWarning("[EnemySpawner] player 미할당(태그 Player 확인)");
        if (mainCam == null) Debug.LogWarning("[EnemySpawner] mainCam 미할당(씬의 MainCamera 확인)");
        if (enemy == null) Debug.LogError("[EnemySpawner] enemy 프리팹 미할당!");

        MakePool();
    }

    void Start()
    {
        // WaveManager가 waveSpawnCount 세팅 후 StartSpawner()를 호출
        if (waveSpawnCount > 0)
        {
            Debug.Log($"[EnemySpawner] Start(): waveSpawnCount={waveSpawnCount} → 자체 시작");
            StartSpawner();
        }
        else
        {
            Debug.Log("[EnemySpawner] Start(): waveSpawnCount=0 → WaveManager 호출 대기");
        }
    }

    public void StartSpawner()
    {
        if (enemy == null)
        {
            Debug.LogError("[EnemySpawner] enemy 프리팹 없음 → 스폰 불가");
            return;
        }
        if (player == null)
        {
            Debug.LogError("[EnemySpawner] player 참조 없음 → 스폰 불가");
            return;
        }

        if (coroutine != null) StopSpawner();
        Debug.Log($"[EnemySpawner] StartSpawner() 시작: 목표 {waveSpawnCount}마리");
        coroutine = StartCoroutine(SpawnEnemy());
    }

    public void StopSpawner()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        curSpawnCount = 0; // 웨이브마다 초기화
        Debug.Log("[EnemySpawner] StopSpawner()");
    }

    private IEnumerator SpawnEnemy()
    {
        int lastCount = curSpawnCount;
        while (curSpawnCount < waveSpawnCount)
        {
            SpawnOutsideView();

            if (curSpawnCount == lastCount)
            {
                Debug.LogWarning("[EnemySpawner] 이번 틱에 스폰 0건 → NavMesh/카메라/플레이어/반경 확인 필요");
            }
            lastCount = curSpawnCount;

            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log("[EnemySpawner] 목표 수만큼 스폰 완료, 코루틴 종료");
    }

    public void MakePool()
    {
        if (enemy == null) return;

        for (int i = 0; i < makePoolCount; i++)
        {
            var e = Instantiate(enemy);
            var agent = e.GetComponent<NavMeshAgent>();
            if (agent) agent.enabled = false;
            if (emptySpawnPoint) e.transform.SetParent(emptySpawnPoint, false);
            e.gameObject.SetActive(false);

            poolQ.Enqueue(e);
            poolSet.Add(e);
        }
        Debug.Log($"[EnemySpawner] 풀 생성: {makePoolCount}개");
    }

    public void SpawnOneImmediate()
    {
        if (player == null) return;
        if (!TryFindSpawnPosition(out var pos))
        {
            Debug.LogWarning("[EnemySpawner] SpawnOneImmediate 실패: 스폰 위치 미확보");
            return;
        }

        var enemyObj = Get();
        if (enemyObj == null) return;

        if (ActivateEnemy(enemyObj, pos))
        {
            curSpawnCount++;
            ActiveEnemyCount++;
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] ActivateEnemy 실패 → 반환");
            Return(enemyObj);
        }
    }

    // 배치 루프
    public void SpawnOutsideView()
    {
        int success = 0;
        for (int i = 0; i < enemyCoSpawnCount; i++)
        {
            if (!TryFindSpawnPosition(out var spawnPos)) continue;

            var enemyObj = Get();
            if (enemyObj == null) continue;

            if (ActivateEnemy(enemyObj, spawnPos))
            {
                curSpawnCount++;
                ActiveEnemyCount++;
                success++;
            }
            else
            {
                Return(enemyObj);
            }
        }

        if (success == 0)
        {
            Debug.LogWarning("[EnemySpawner] SpawnOutsideView: 이번 틱 성공 0건");
        }
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPos)
    {
        if (player == null)
        {
            spawnPos = Vector3.zero;
            return false;
        }

        for (int safety = 0; safety < 50; safety++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = player.position + dir * spawnRadius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 50f, NavMesh.AllAreas))
            {
                if (mainCam == null || !IsOnScreen(mainCam, hit.position))
                {
                    spawnPos = hit.position;
                    return true;
                }
            }
        }

        // 폴백: NavMesh가 아예 없을 때, 바닥 y를 플레이어와 동일하게 강제 스폰(테스트용)
        // (원한다면 이 폴백을 끄세요)
        Vector3 fallback = player.position + Random.onUnitSphere * spawnRadius;
        fallback.y = player.position.y;
        if (mainCam == null || !IsOnScreen(mainCam, fallback))
        {
            Debug.LogWarning("[EnemySpawner] NavMesh.SamplePosition 실패 → 폴백 위치 사용");
            spawnPos = fallback;
            return true;
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

    public GameObject Get()
    {
        if (poolQ.Count <= 0) MakePool();
        if (poolQ.Count == 0) return null;

        var e = poolQ.Dequeue();
        poolSet.Remove(e);
        return e;
    }

    public void Return(GameObject e)
    {
        if (!e) return;

        if (!poolSet.Add(e)) return;

        var le = e.GetComponent<LivingEntity>();
        if (le)
        {
            UnhookDeath(le);
            Unregister(le);
        }

        e.SetActive(false);
        if (emptySpawnPoint) e.transform.SetParent(emptySpawnPoint, false);

        poolQ.Enqueue(e);

        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
        CheckWaveCleared();
    }

    private void CheckWaveCleared()
    {
        if (IsWaveCleared)
        {
            Debug.Log("[EnemySpawner] 웨이브 클리어");
            OnWaveCleared?.Invoke();
        }
    }

    private bool ActivateEnemy(GameObject enemyObj, Vector3 pos)
    {
        // NavMesh 위치 보정
        Vector3 finalPos = pos;
        if (NavMesh.SamplePosition(pos, out var hit, 5f, NavMesh.AllAreas))
            finalPos = hit.position;

        enemyObj.transform.SetPositionAndRotation(
            finalPos,
            (player != null)
                ? Quaternion.LookRotation((player.position - finalPos).normalized, Vector3.up)
                : Quaternion.identity
        );

        var agent = enemyObj.GetComponent<NavMeshAgent>();
        if (agent)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.Warp(finalPos);
                agent.isStopped = false;
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] NavMeshAgent 있음, isOnNavMesh=false");
            }
        }

        enemyObj.SetActive(true);

        var le = enemyObj.GetComponent<LivingEntity>();
        if (le)
        {
            Register(le);
            HookDeath(le, enemyObj);
            // le.ResetOrRevive();
        }

        return true;
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
        if (dead != null && deathMap.TryGetValue(dead, out var go))
        {
            Return(go);
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] onDeath 매핑 없음(이미 반환됐을 수 있음)");
        }
    }

    public void DebugSpawn(int count)
    {
        for (int i = 0; i < count; i++)
            SpawnOneImmediate();
    }
}
