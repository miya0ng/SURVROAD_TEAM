using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public Transform emptySpawnPoint;
    public GameObject enemy;

    public int curSpawnCount { get; set; }
    public int waveSpawnCount { get; set; }

    private int makePoolCount = 30;
    private int enemyCoSpawnCount = 3;

    private Queue<GameObject> EnemyPool = new Queue<GameObject>();

    public int ActiveEnemyCount { get; set;}
    //private Queue<GameObject> copyAllEnemy = new();

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

    public Coroutine coroutine { get; set; }
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

    public void MakePool()
    {
        for (int i = 0; i < makePoolCount; i++)
        {
            var e = Instantiate(enemy, emptySpawnPoint);
            e.gameObject.SetActive(false);
            EnemyPool.Enqueue(e);
        }
    }

    void Update()
    {
           
    }

    public void SpawnOutsideView()
    {
        for (int i = 0; i < enemyCoSpawnCount; i++)
        {
            Vector3 pos = GetSpawnPositionOutsideCamera();

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                var newPos = hit.position;
                var enemy = Get();
                enemy.transform.position = newPos;
            }
            curSpawnCount++;
            ActiveEnemyCount++;
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
        if(EnemyPool.Count <= 0)
        {
            MakePool();
            //EnemyPool = new Queue<GameObject>(copyAllEnemy);
            return null;
        }
        //Debug.Log("Spawn");

        var e = EnemyPool.Dequeue();
        e.gameObject.SetActive(true);
        return e;
    }
    public void Return(GameObject e)
    {
        EnemyPool.Enqueue(e);
        e.gameObject.SetActive(false);
    }
}