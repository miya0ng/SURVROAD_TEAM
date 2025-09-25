using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private WaveManager waveManager;
    private EnemySpawner enemySpawner;

    public int specialPartCount = 0;

    [SerializeField]
    private float playTime = 0f;

    private bool isGameOver = false;
    private bool subscribed = false;

    void Awake()
    {
        var esObj = GameObject.FindGameObjectWithTag("EnemySpawner");
        if (esObj) enemySpawner = esObj.GetComponent<EnemySpawner>();

        var wmObj = GameObject.FindGameObjectWithTag("WaveManager");
        if (wmObj) waveManager = wmObj.GetComponent<WaveManager>();

        if (enemySpawner && waveManager)
        {
            enemySpawner.OnWaveCleared += HandleWaveCleared;
            subscribed = true;
        }
        else
        {
            Debug.LogError("[GameManager] EnemySpawner 또는 WaveManager 참조 실패");
        }
    }

    void OnDestroy()
    {
        if (subscribed && enemySpawner)
            enemySpawner.OnWaveCleared -= HandleWaveCleared;
        subscribed = false;
    }

    void Update()
    {
        // 이전 코드:
        // if (enemySpawner.curSpawnCount >= enemySpawner.waveSpawnCount && enemySpawner.ActiveEnemyCount <= 0)
        //     waveManager.NextWave();

        playTime += Time.deltaTime;

        if (Input.anyKey && isGameOver)
        {
            GameStart();
        }
    }

    private void HandleWaveCleared()
    {
        if (!waveManager) return;
        waveManager.NextWave();
    }

    public void AddSpecialPart(int amount = 1)
    {
        specialPartCount += amount;
        OnSpecialPartChanged?.Invoke(specialPartCount);
    }

    public void ResetSpecialParts()
    {
        specialPartCount = 0;
        OnSpecialPartChanged?.Invoke(specialPartCount);
    }

    public event System.Action<int> OnSpecialPartChanged;

    public void GameStart()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        if (enemySpawner) enemySpawner.StopSpawner();
        Debug.Log($"Game Over! Total Play Time: {playTime} seconds.");
    }
}
