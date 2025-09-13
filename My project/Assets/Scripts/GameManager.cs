using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public UiDebugTexts debugText;
    [SerializeField]
    private float playTime = 0f;

    public int waveCount = 1;
    public int leftEnemyCount;
    private bool isGameOver = false;
    void Start()
    {

        //leftEnemyCount = 
    }
    void Update()
    {
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
        waveCount = 1;
        Time.timeScale = 0f; // Pause the game

        Debug.Log($"Game Over! Total Play Time: {playTime} seconds.");
        // Additional game over logic can be added here
    }

    public bool WaveClear()
    {
        if (leftEnemyCount != 0)
            return false;
        else
        {
            Debug.Log($"Wave {waveCount} cleared!");
            waveCount++;
            // Additional wave clear logic can be added here
            return true;
        }
    }
}
