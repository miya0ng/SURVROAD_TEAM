using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;

    [SerializeField] private Transform[] spawnPoints;

    public Transform emptySpawnPoint;
    public GameObject enemy;

    public int enemySpawnCount = 5;

    private readonly Queue<GameObject> pool = new();
    [SerializeField] private int enemyPoolSize = 10;

    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float cameraMargin = 0.1f;

    private float spawnInterval = 5f;
    private float spawnTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        gameCamera = Camera.main;
        MakePool();
    }

    private void MakePool()
    {
        for (int i = 0; i < enemyPoolSize; i++)
        {
            var e = Instantiate(enemy, emptySpawnPoint);
            e.gameObject.SetActive(false);
            pool.Enqueue(e);
        }
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnOutsideView();
            spawnTimer = spawnInterval;
        }
    }

    public void SpawnOutsideView()
    {
        for(int i = 0; i < enemySpawnCount; i++)
        {
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
            return null;
            //MakePool();
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
}