using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public UiDebugTexts debugText;

    private WaveManager waveManager;
    private EnemySpawner enemySpawner;

    [SerializeField]
    private float playTime = 0f;

    private bool isGameOver = false;

    void Awake()
    {
        enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner").GetComponent<EnemySpawner>();
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
    }

    void Start()
    {
        waveManager.NextWave();
    }
    void Update()
    {
        //Debug.Log(enemySpawner.EnemyPoolSize);
        Debug.Log(enemySpawner.ActiveEnemyCount);
        if (enemySpawner.ActiveEnemyCount <= 0 && Input.GetKeyDown(KeyCode.C))
        {
            waveManager.NextWave();
        }
        playTime += Time.deltaTime;
        
        if (Input.anyKey && isGameOver)
        {
            GameStart();
        }
    }

    public void GameStart()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    public void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f; // Pause the game
        enemySpawner.StopSpawner();
        Debug.Log($"Game Over! Total Play Time: {playTime} seconds.");
        // Additional game over logic can be added here
    }
}
