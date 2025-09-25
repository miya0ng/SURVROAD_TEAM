using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform emptySpawnPoint;
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform player;
    
     private string enemyCarCsvPath = "EnemyCarTable";
     private string prefabBasePath = "Enemies";            // Resources/Enemies/PrefabName.prefab

    [Header("Spawn Settings (defaults/overrides)")]
    [SerializeField] private float spawnRadius = 100f;
    [SerializeField] private int makePoolCount = 50;
    [SerializeField] private int enemyCoSpawnCount = 1;
    [SerializeField] private float spawnInterval = 0.5f;

    public int WaveTotalToSpawn => _waveTotalToSpawn;
    public int SpawnedInWave => _spawnedInWave;
    public bool IsWaveCleared => _spawnedInWave >= _waveTotalToSpawn && ActiveEnemyCount <= 0;
    public event System.Action OnWaveCleared;

    // 내부 상태
    private readonly Dictionary<int, GameObject> _idToPrefab = new();
    private readonly Dictionary<int, Queue<GameObject>> _poolQ = new();
    private readonly Dictionary<int, HashSet<GameObject>> _poolSet = new();
    private readonly Dictionary<GameObject, int> _instanceToId = new();

    private readonly List<LivingEntity> enemies = new();
    public List<LivingEntity> GetEnemies() => enemies;

    public int ActiveEnemyCount { get; private set; }
    public Coroutine coroutine { get; private set; }

    private WaveData _currentWave;
    private int _waveTotalToSpawn;
    private IEnumerator<(int enemyID, int batch)> _batchEnumerator;
    private int _spawnedInWave;

    private readonly Dictionary<LivingEntity, GameObject> deathMap = new();

    private EnemyCarDataTable enemyCarTable;

    public void Register(LivingEntity e) { if (e && !enemies.Contains(e)) enemies.Add(e); }
    public void Unregister(LivingEntity e) { if (e) enemies.Remove(e); }

    void Awake()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (mainCam == null) mainCam = Camera.main;

        // 적 차량 데이터 테이블 로드
        enemyCarTable = new EnemyCarDataTable();
        enemyCarTable.Load(enemyCarCsvPath);

        if (player == null) Debug.LogWarning("[EnemySpawner] player 미할당(태그 Player 확인)");
        if (mainCam == null) Debug.LogWarning("[EnemySpawner] mainCam 미할당(씬의 MainCamera 확인)");
    }

    // ========= 웨이브 주입 =========
    public void SetWave(WaveData wave, int? coSpawnOverride = null, float? intervalOverride = null)
    {
        _currentWave = wave;
        _waveTotalToSpawn = wave?.TotalAmount ?? 0;
        _spawnedInWave = 0;

        if (coSpawnOverride.HasValue) enemyCoSpawnCount = Mathf.Max(1, coSpawnOverride.Value);
        if (intervalOverride.HasValue) spawnInterval = Mathf.Max(0.01f, intervalOverride.Value);

        // 현재 웨이브에 필요한 ID만 로드/매핑
        BuildIdToPrefabForWave(wave);

        _batchEnumerator = WaveDataTable.BuildBatches(_currentWave, enemyCoSpawnCount).GetEnumerator();

        // ID별 풀 준비/보충
        PrewarmPoolsForWave(_currentWave);

        StopSpawner();
        StartSpawner();
    }

    private void BuildIdToPrefabForWave(WaveData wave)
    {
        if (wave == null) return;

        foreach (var (enemyID, _) in wave.GetEnemySlots())
        {
            if (_idToPrefab.ContainsKey(enemyID)) continue;

            var car = enemyCarTable.GetEnemyCarData(enemyID);
            if (car == null)
            {
                Debug.LogError($"[EnemySpawner] EnemyID {enemyID} 데이터 없음(EnemyCarTable)");
                continue;
            }
            if (string.IsNullOrEmpty(car.PrefabName))
            {
                Debug.LogError($"[EnemySpawner] EnemyID {enemyID} PrefabName 비어있음");
                continue;
            }

            // Resources/Enemies/PrefabName
            var path = string.IsNullOrEmpty(prefabBasePath) ? car.PrefabName : $"{prefabBasePath}/{car.PrefabName}";
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[EnemySpawner] Resources.Load 실패: {path} (EnemyID:{enemyID}, Name:{car.Name})");
                continue;
            }
            _idToPrefab[enemyID] = prefab;
        }

        if (_idToPrefab.Count == 0)
            Debug.LogError("[EnemySpawner] 현재 웨이브에 필요한 프리팹을 하나도 로드하지 못했습니다.");
    }

    private void PrewarmPoolsForWave(WaveData wave)
    {
        if (wave == null) return;
        foreach (var (enemyID, amount) in wave.GetEnemySlots())
            EnsurePool(enemyID, Mathf.Max(makePoolCount, Mathf.Min(amount, makePoolCount)));
    }

    // ========= 스폰 제어 =========
    public void StartSpawner()
    {
        if (_currentWave == null || _waveTotalToSpawn <= 0)
        {
            Debug.LogError("[EnemySpawner] 현재 웨이브 정보 없음/총량 0 → 시작 불가");
            return;
        }
        if (player == null)
        {
            Debug.LogError("[EnemySpawner] player 참조 없음 → 스폰 불가");
            return;
        }

        if (coroutine != null) StopSpawner();
        coroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawner()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        Debug.Log("[EnemySpawner] StopSpawner()");
    }

    private IEnumerator SpawnRoutine()
    {
        int lastSpawned = _spawnedInWave;

        while (_spawnedInWave < _waveTotalToSpawn)
        {
            int success = SpawnOneBatchTick();

            if (success == 0 && lastSpawned == _spawnedInWave)
                Debug.LogWarning("[EnemySpawner] 이번 틱 스폰 0건 → NavMesh/카메라/반경/풀 확인");

            lastSpawned = _spawnedInWave;
            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("[EnemySpawner] 목표 수만큼 스폰 완료");
    }

    // 배치 하나(한 틱) 스폰
    private int SpawnOneBatchTick()
    {
        if (_batchEnumerator == null) return 0;
        int spawnedThisTick = 0;

        if (_batchEnumerator.MoveNext())
        {
            var (enemyID, batch) = _batchEnumerator.Current;

            for (int i = 0; i < batch; i++)
            {
                if (!TryFindSpawnPosition(out var pos)) break;
                var enemyObj = Get(enemyID);
                if (enemyObj == null) break;

                if (ActivateEnemy(enemyObj, pos, enemyID))
                {
                    _spawnedInWave++;
                    ActiveEnemyCount++;
                    spawnedThisTick++;
                }
                else
                {
                    Return(enemyID, enemyObj);
                }
            }
        }
        return spawnedThisTick;
    }

    // ========= 풀 관리 (ID별) =========
    private void EnsurePool(int enemyID, int initialCount)
    {
        if (!_idToPrefab.TryGetValue(enemyID, out var prefab) || prefab == null)
        {
            Debug.LogError($"[EnemySpawner] EnemyID {enemyID} 프리팹 매핑 없음");
            return;
        }

        if (!_poolQ.ContainsKey(enemyID))
        {
            _poolQ[enemyID] = new Queue<GameObject>();
            _poolSet[enemyID] = new HashSet<GameObject>();
        }

        int need = Mathf.Max(0, initialCount - _poolQ[enemyID].Count);
        for (int i = 0; i < need; i++)
        {
            var e = Instantiate(prefab);
            var agent = e.GetComponent<NavMeshAgent>();
            if (agent) agent.enabled = false;
            if (emptySpawnPoint) e.transform.SetParent(emptySpawnPoint, false);
            e.SetActive(false);

            _poolQ[enemyID].Enqueue(e);
            _poolSet[enemyID].Add(e);
            _instanceToId[e] = enemyID; // 역매핑 등록
        }
    }

    public GameObject Get(int enemyID)
    {
        EnsurePool(enemyID, makePoolCount);

        var q = _poolQ[enemyID];
        var set = _poolSet[enemyID];

        if (q.Count == 0)
        {
            EnsurePool(enemyID, makePoolCount);
            if (q.Count == 0) return null;
        }

        var e = q.Dequeue();
        set.Remove(e);
        _instanceToId[e] = enemyID;
        return e;
    }

    public void Return(int enemyID, GameObject e)
    {
        if (!e) return;

        if (_poolSet.TryGetValue(enemyID, out var set))
        {
            if (!set.Add(e)) return;
        }

        var le = e.GetComponent<LivingEntity>();
        if (le)
        {
            UnhookDeath(le);
            Unregister(le);
        }

        e.SetActive(false);
        if (emptySpawnPoint) e.transform.SetParent(emptySpawnPoint, false);

        if (_poolQ.TryGetValue(enemyID, out var q))
            q.Enqueue(e);

        _instanceToId[e] = enemyID;
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

    // ========= 스폰 공통 로직 =========
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

        // 폴백
        Vector3 fallback = player.position + Random.onUnitSphere * spawnRadius;
        fallback.y = player.position.y;
        if (mainCam == null || !IsOnScreen(mainCam, fallback))
        {
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

    private bool ActivateEnemy(GameObject enemyObj, Vector3 pos, int enemyID)
    {
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

        var data = enemyCarTable?.GetEnemyCarData(enemyID);
        if (data != null)
        {
            var wrapper = new EnemyCarDataWrapper(data);

            // EnemySetup 있으면 그 경로로(행동 프리셋까지)
            var setup = enemyObj.GetComponent<EnemySetup>();
            if (setup != null)
            {
                setup.Apply(wrapper);
            }
            else
            {
                // 없으면 최소 파라미터만 직접 적용
                var rb = enemyObj.GetComponent<Rigidbody>();
                wrapper.ApplyTo(agent, rb, agentAsKinematic: true);
            }
        }

        enemyObj.SetActive(true);

        var le = enemyObj.GetComponent<LivingEntity>();
        if (le)
        {
            Register(le);
            HookDeath(le, enemyObj);
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
            if (!_instanceToId.TryGetValue(go, out var id))
            {
                Debug.LogWarning("[EnemySpawner] instance->id 역매핑 없음, 안전빵 0으로 처리");
                id = 0;
            }
            Return(id, go);
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] onDeath 매핑 없음(이미 반환됐을 수 있음)");
        }
    }
}
