using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UiDebugTexts debugText;
    [SerializeField]
    private float playTime = 0f;

    public int waveCount = 0;
    public int leftEnemyCount;

    void Start()
    {

        //leftEnemyCount = 
    }
    void Update()
    {
        playTime += Time.deltaTime;
        debugText.playTime.text = $"Play Time: {playTime:F2} seconds";
    }

    public void GameStart()
    {
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        waveCount = 0;
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
