using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private EnemySpawner enemySpawner;

    public int currentWave;
    private int maxWaves = 3;

    public int[] enemiesPerWave = { 2, 3, 4 };
    public float WaveTimer { get; set; }

    public void Awake()
    {
        currentWave = 0;
        enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemySpawner>();
    }

    public void Start()
    {

    }
    public void Update()
    {
        WaveTimer += Time.deltaTime;
    }
    public void NextWave()
    {
        if (currentWave == maxWaves)
        {
            return;
        }

        WaveTimer = 0;
        currentWave++;
        enemySpawner.waveSpawnCount = enemiesPerWave[currentWave - 1];

        enemySpawner.StopSpawner();
        enemySpawner.StartSpawner();
    }
}