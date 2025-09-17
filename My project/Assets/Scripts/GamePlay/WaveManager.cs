using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private GameManager gameManager;
    private EnemySpawner enemySpawner;

    public int currentWave;
    private int maxWaves = 3;

    private int[] enemiesPerWave = { 20, 30, 40 };
    public float WaveTimer { get; set; }

    public void Awake()
    {
        currentWave = 0;
        enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemySpawner>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
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
        enemySpawner.StopSpawner();
        enemySpawner.StartSpawner();

        enemySpawner.curSpawnCount = 0;
        WaveTimer = 0;
        currentWave++;
        enemySpawner.waveSpawnCount = enemiesPerWave[currentWave - 1];
    }
}