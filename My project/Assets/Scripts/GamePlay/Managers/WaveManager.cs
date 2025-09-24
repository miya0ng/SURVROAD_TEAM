using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private EnemySpawner enemySpawner;

    public int currentWave;
    private int maxWaves = 3;

    public int[] enemiesPerWave = { 10, 15, 20 };
    public float WaveTimer { get; set; }

    public void Awake()
    {
        currentWave = 0;
        enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemySpawner>();
    }

    public void Start()
    {
        NextWave();

        enemySpawner.OnWaveCleared += NextWave;
    }

    public void Update()
    {
        WaveTimer += Time.deltaTime;
    }

    public void NextWave()
    {
        if (currentWave >= maxWaves)
        {
            return;
        }

        WaveTimer = 0f;
        currentWave++;

        enemySpawner.waveSpawnCount = enemiesPerWave[currentWave - 1];

        enemySpawner.StopSpawner();
        enemySpawner.StartSpawner();
    }
}
