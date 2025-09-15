using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private GameManager gameManager;
    private EnemySpawner enemySpawner;

    public int currentWave = 0;
    private int MaxWaves = 3;

    private int[] enemiesPerWave = { 2, 3, 4 };

    public void Awake()
    {
        enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemySpawner>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }
    public void NextWave()
    {
        if(currentWave <= MaxWaves)
        {
            currentWave++;
            gameManager.waveCount = currentWave;
            enemySpawner.EnemyPoolSize = enemiesPerWave[currentWave - 1];
            enemySpawner.EnemySpawnCount = enemySpawner.EnemyPoolSize;
            gameManager.leftEnemyCount = 0;
        }
        else
        {
            return;
        }
    }
}