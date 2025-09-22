using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform emptySpawnPoint;
    [SerializeField] private GameObject enemy;
    [SerializeField] private Camera mainCam;     // Inspector에서 MainCamera 직접 할당
    [SerializeField] private Transform player;
     private float spawnRadius = 300f;
     private int makePoolCount = 200;
     private int enemyCoSpawnCount = 1;
     private float spawnInterval = 1f;

    private Queue<GameObject> EnemyPool = new Queue<GameObject>();
    private List<LivingEntity> enemies = new List<LivingEntity>();
    public List<LivingEntity> GetEnemies() => enemies;
    public int curSpawnCount { get; private set; }
    public int waveSpawnCount { get; set; }
    public int ActiveEnemyCount { get; private set; }
    public Coroutine coroutine { get; private set; }

    public void Register(LivingEntity enemy) => enemies.Add(enemy);
    public void Unregister(LivingEntity enemy) => enemies.Remove(enemy);

    void Awake()
    {
        if (mainCam == null)
            Debug.LogWarning("EnemySpawner: mainCam 할당");
        MakePool();
    }

    private void Start()
    {
        StartSpawner();
    }

    public void StartSpawner()
    {
        coroutine = StartCoroutine(SpawnEnemy());
    }

    public void StopSpawner()
    {
        if (coroutine != null)
        {
            curSpawnCount = 0;
            StopCoroutine(coroutine);
        }
    }

    private IEnumerator SpawnEnemy()
    {
        while (curSpawnCount < waveSpawnCount)
        {
            Debug.Log("Spawn");
            SpawnOutsideView();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void MakePool()
    {
        for (int i = 0; i < makePoolCount; i++)
        {
            var e = Instantiate(enemy);
            e.transform.SetParent(emptySpawnPoint);
            e.gameObject.SetActive(false);
            EnemyPool.Enqueue(e);
        }
    }

    public void SpawnOutsideView()
    {
        for (int i = 0; i < enemyCoSpawnCount; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool found = false;

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
                        found = true;
                        break;
                    }
                }
            }

            if (found)
            {
                var enemyObj = Get();
                if (enemyObj != null)
                {
                    enemyObj.transform.position = spawnPos;
                    curSpawnCount++;
                    ActiveEnemyCount++;
                }
            }
        }
    }
    private bool IsOnScreen(Camera cam, Vector3 worldPos)
    {
        if (cam == null) return false;

        Vector3 screenPos = cam.WorldToViewportPoint(worldPos);

        return (screenPos.z > 0 &&
                screenPos.x >= 0 && screenPos.x <= 1 &&
                screenPos.y >= 0 && screenPos.y <= 1);
    }
    public GameObject Get()
    {
        if (EnemyPool.Count <= 0)
            MakePool();

        if (EnemyPool.Count == 0) return null;

        var e = EnemyPool.Dequeue();
        e.gameObject.SetActive(true);
        Register(e.GetComponent<LivingEntity>());
        return e;
    }

    public void Return(GameObject e)
    {
        EnemyPool.Enqueue(e);
        e.gameObject.SetActive(false);
        Unregister(e.GetComponent<LivingEntity>());
        ActiveEnemyCount--;
    }
}