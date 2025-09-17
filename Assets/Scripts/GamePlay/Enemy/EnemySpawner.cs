using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public Transform emptySpawnPoint;
    public GameObject enemy;

    private int enemySpawnCount = 1;
    public int EnemySpawnCount
    {
        get { return enemySpawnCount; }
        set { enemySpawnCount = value; }
    }

    private Queue<GameObject> pool = new();
    private Queue<GameObject> acticveEnemyPool = new();
    private Queue<GameObject> copyAllEnemy = new();

    private int enemyPoolSize;
    public int EnemyPoolSize
    {
        get { return enemyPoolSize; }
        set { enemyPoolSize = value; }
    }

    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float cameraMargin = 0.1f;

    private float spawnInterval = 1f;

    public Coroutine coroutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        gameCamera = Camera.main;
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
        if(coroutine != null)
        StopCoroutine(coroutine);
    }
    private IEnumerator SpawnEnemy()
    {
        SpawnOutsideView();
        yield return new WaitForSeconds(spawnInterval);
    }
    public void Register(GameObject enemy)
    {
        acticveEnemyPool.Enqueue(enemy);
    }
    public void UnRegister(GameObject enemy)
    {
        if(acticveEnemyPool != null)
        {
            acticveEnemyPool.Dequeue();
        }
    }
    public void MakePool()
    {
        for (int i = 0; i < 30; i++)
        {
            var e = Instantiate(enemy, emptySpawnPoint);
            e.gameObject.SetActive(false);
            pool.Enqueue(e);
            copyAllEnemy.Enqueue(e);
        }
    }

    void Update()
    {
           
    }

    public void SpawnOutsideView()
    {
        for(int i = 0; i < EnemySpawnCount; i++)
        {
            if (enemyPoolSize <= 0)
            {
                return;
            }

            Vector3 pos = GetSpawnPositionOutsideCamera();

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                var newPos = hit.position;
                var enemy = Get();
                enemy.transform.position = newPos;
            }
        }
    }

    private Vector3 GetSpawnPositionOutsideCamera()
    {
        Vector3 pos;
        int safety = 0;

        do
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            pos = player.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            Vector3 vp = gameCamera.WorldToViewportPoint(pos);

            bool insideCamera =
                vp.z > 0 &&
                vp.x > -cameraMargin && vp.x < 1 + cameraMargin &&
                vp.y > -cameraMargin && vp.y < 1 + cameraMargin;

            if (!insideCamera)
                break;

            safety++;
        }
        while (safety < 50);

        return pos;
    }
    public GameObject Get()
    {
        if(pool.Count == 0)
        {
            //MakePool();
            pool = new Queue<GameObject>(copyAllEnemy);
            return null;
        }
        var e = pool.Dequeue();

        e.gameObject.SetActive(true);
        return e;
    }
    public void Return(GameObject e)
    {
        e.gameObject.SetActive(false);
        pool.Enqueue(e);
    }

    public int GetActiveEnemyPoolCount()
    {
        return acticveEnemyPool.Count;
    }
}