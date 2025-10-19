using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private WaveManager waveManager;
    private EnemyManager enemySpawner;

    public int specialPartCount = 0;

    [SerializeField]
    public float playTime = 0f;

    private bool isGameOver = false;
    private bool subscribed = false;
    public event System.Action<bool> OnGameOver;
    void Awake()
    {
        var esObj = GameObject.FindGameObjectWithTag("EnemySpawner");
        if (esObj) enemySpawner = esObj.GetComponent<EnemyManager>();

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
        playTime += Time.deltaTime;
    }

    private void HandleWaveCleared()
    {
        //StageClear();
        // TODO: 클리어 연출/사운드, 점수 정산 등만 처리
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
        SceneLoader.I?.Load(currentScene);
    }
    public void StageClear()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;
        if (enemySpawner) enemySpawner.StopSpawner();
        Debug.Log($"Stage Clear! Total Play Time: {playTime} seconds.");
        OnGameOver?.Invoke(true);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Time.timeScale = 0f;
        if (enemySpawner) enemySpawner.StopSpawner();
        Debug.Log($"Game Over! Total Play Time: {playTime} seconds.");
        OnGameOver?.Invoke(false);
    }
}