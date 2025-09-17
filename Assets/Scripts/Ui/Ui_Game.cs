using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Ui_Game : MonoBehaviour
{
    private GameManager gameManager;

    public EnemySpawner enemySpawner;
    public TextMeshProUGUI leftEnemy;
    public TextMeshProUGUI wave;

    public TextMeshProUGUI playTime;
    public void Awake()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    public void Start()
    {
        
    }

    public void Update()
    {
        UpdateLeftEnemyText();
        playTime.text = $"{Time.timeSinceLevelLoad:F2}";
        wave.text = $"Wave: {gameManager.waveCount}";
    }


    public void UpdateLeftEnemyText()
    {
        var count = enemySpawner.GetActiveEnemyPoolCount();
        leftEnemy.text = $"Left Enemy: {count}";
    }
}