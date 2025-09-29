using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Pathfinding; // A* Pathfinding Project
public class EnemySpawner : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private Transform emptySpawnPoint;
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform player;
    
     private string enemyCarCsvPath = "EnemyCarTable";
     private string prefabBasePath = "Enemies";            // Resources/Enemies/PrefabName.prefab

    [Header("Spawn Settings (defaults/overrides)")]
    [SerializeField] private float spawnRadius = 50f;
    [SerializeField] private int makePoolCount = 50;
    [SerializeField] private int enemyCoSpawnCount = 1;
    [SerializeField] private float spawnInterval = 0.5f;

    public int WaveTotalToSpawn => _waveTotalToSpawn;
    public int SpawnedInWave => _spawnedInWave;
    public bool IsWaveCleared => _spawnedInWave >= _waveTotalToSpawn && ActiveEnemyCount <= 0;
    public event System.Action OnWaveCleared;

    // ���� ����
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

    private EnemyDataTable enemyDataTable;

    public void Register(LivingEntity e) { if (e && !enemies.Contains(e)) enemies.Add(e); }
    public void Unregister(LivingEntity e) { if (e) enemies.Remove(e); }

    void Awake()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (mainCam == null) mainCam = Camera.main;

        // �� ���� ������ ���̺� �ε�
        enemyDataTable = new EnemyDataTable();
        enemyDataTable.Load(enemyCarCsvPath);

        if (player == null) Debug.LogWarning("[EnemySpawner] player ���Ҵ�(�±� Player Ȯ��)");
        if (mainCam == null) Debug.LogWarning("[EnemySpawner] mainCam ���Ҵ�(���� MainCamera Ȯ��)");
    }

    public void SetWave(WaveData wave, int? coSpawnOverride = null, float? intervalOverride = null)
    {
        _currentWave = wave;
        _waveTotalToSpawn = wave?.TotalAmount ?? 0;
        _spawnedInWave = 0;

        if (coSpawnOverride.HasValue) enemyCoSpawnCount = Mathf.Max(1, coSpawnOverride.Value);
        if (intervalOverride.HasValue) spawnInterval = Mathf.Max(0.01f, intervalOverride.Value);

        BuildIdToPrefabForWave(wave);

        _batchEnumerator = WaveDataTable.BuildBatches(_currentWave, enemyCoSpawnCount).GetEnumerator();

        PrewarmPoolsForWave(_currentWave);

        StopSpawner();
        StartSpawner();
    }
    private GameObject ResolvePrefab(EnemySpec spec)
    {
        var file = string.IsNullOrEmpty(spec.PrefabName) ? spec.Name : spec.PrefabName;
        var path = string.IsNullOrEmpty(prefabBasePath) ? file : $"{prefabBasePath}/{file}";
        var prefab = Resources.Load<GameObject>(path);
        if (!prefab) Debug.LogError($"Prefab not found at {path} for EnemyID:{spec.Id} ({spec.Name})");
        return prefab;
    }
    private void BuildIdToPrefabForWave(WaveData wave)
    {
        if (wave == null) return;

        foreach (var (enemyID, _) in wave.GetEnemySlots())
        {
            if (_idToPrefab.ContainsKey(enemyID)) continue;

            if (!enemyDataTable.TryGet(enemyID, out var spec))
            {
                Debug.LogError($"[EnemySpawner] EnemyID {enemyID} ���� ����(EnemyDataTable)");
                continue;
            }

            var prefab = ResolvePrefab(spec);
            if (!prefab) continue;

            _idToPrefab[enemyID] = prefab;
        }

        if (_idToPrefab.Count == 0)
            Debug.LogError("[EnemySpawner] ���� ���̺�� ������ �ε� ����");
    }

    private void PrewarmPoolsForWave(WaveData wave)
    {
        if (wave == null) return;
        foreach (var (enemyID, amount) in wave.GetEnemySlots())
            EnsurePool(enemyID, Mathf.Max(makePoolCount, Mathf.Min(amount, makePoolCount)));
    }

    // ========= ���� ���� =========
    public void StartSpawner()
    {
        if (_currentWave == null || _waveTotalToSpawn <= 0)
        {
            Debug.LogError("[EnemySpawner] ���� ���̺� ���� ����/�ѷ� 0 �� ���� �Ұ�");
            return;
        }
        if (player == null)
        {
            Debug.LogError("[EnemySpawner] player ���� ���� �� ���� �Ұ�");
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
                Debug.LogWarning("[EnemySpawner] �̹� ƽ ���� 0�� �� NavMesh/ī�޶�/�ݰ�/Ǯ Ȯ��");

            lastSpawned = _spawnedInWave;
            yield return new WaitForSeconds(spawnInterval);
        }

        Debug.Log("[EnemySpawner] ��ǥ ����ŭ ���� �Ϸ�");
    }

    // ��ġ �ϳ�(�� ƽ) ����
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

    // ========= Ǯ ���� (ID��) =========
    private void EnsurePool(int enemyID, int initialCount)
    {
        if (!_idToPrefab.TryGetValue(enemyID, out var prefab) || prefab == null)
        {
            Debug.LogError($"[EnemySpawner] EnemyID {enemyID} ������ ���� ����");
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
            _instanceToId[e] = enemyID; // ������ ���
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
            Debug.Log("[EnemySpawner] ���̺� Ŭ����");
            OnWaveCleared?.Invoke();
        }
    }

    // ========= ���� ���� ���� =========
        private bool TryFindSpawnPosition(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;
        if (player == null || AstarPath.active == null) return false;

        var nn = NNConstraint.Default;
        nn.constrainWalkability = true;
        nn.walkable = true;

        for (int safety = 0; safety < 40; safety++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = player.position + dir * spawnRadius;

            var nnInfo = AstarPath.active.GetNearest(candidate, nn);
            var node = nnInfo.node;

            if (node != null && node.Walkable)
            {
                var pos = (Vector3)nnInfo.position;
                if (mainCam == null || !IsOnScreen(mainCam, pos))
                {
                    spawnPos = pos;
                    return true;
                }
            }
        }
        return false;
    }
   private bool IsOnScreen(Camera cam, Vector3 worldPos)
    {
        if (!cam) return false;
        Vector3 v = cam.WorldToViewportPoint(worldPos);
        return (v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f);
    }


    private bool ActivateEnemy(GameObject enemyObj, Vector3 pos, int enemyID)
    {
        // 1) ��ġ ���� (A*)
        var ai = enemyObj.GetComponent<IAstarAI>();
        if (ai != null) { ai.Teleport(pos, true); ai.canMove = true; ai.isStopped = false; }
        else
        {
            enemyObj.transform.SetPositionAndRotation(
                pos,
                player ? Quaternion.LookRotation((player.position - pos).normalized, Vector3.up) : Quaternion.identity
            );
        }

        // 2) ���� ����
        if (enemyDataTable != null && enemyDataTable.TryGet(enemyID, out var spec))
        {
            // �⺻ ������Ʈ���� �ݿ� (ü��/����/�ѱ�)
            var le = enemyObj.GetComponent<LivingEntity>();
            if (le) { le.maxHp = spec.Durability; le.curHp = spec.Durability; }

            var car = enemyObj.GetComponent<EnemyCarController>();
            if (car) { car.maxSpeed = spec.MaxSpeed; car.accel = spec.Accel; car.handling = Mathf.Max(0.5f, spec.Handling); }

            var gun = enemyObj.GetComponent<EnemyGunController>();
            if (gun) { gun.damage = Mathf.Max(1, spec.AttackDamage); gun.fireInterval = Mathf.Max(0.1f, spec.AttackInterval); }

            // �� EnemyDriver���� ���� ���� ���� (����)
            var driver = enemyObj.GetComponent<EnemyDriver>();
            if (driver)
            {
                driver.SetEnemyId(enemyID);
                if (player) driver.SetTarget(player);
                driver.ApplySpec(spec);
            }
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] EnemyID {enemyID} ���� ������");
        }

        enemyObj.SetActive(true);

        var ent = enemyObj.GetComponent<LivingEntity>();
        if (ent) { Register(ent); HookDeath(ent, enemyObj); }

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
                Debug.LogWarning("[EnemySpawner] instance->id ������ ����, ������ 0���� ó��");
                id = 0;
            }
            Return(id, go);
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] onDeath ���� ����(�̹� ��ȯ���� �� ����)");
        }
    }
}
