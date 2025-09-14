using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Ui_Game : MonoBehaviour
{
    public EnemyManager enemyManager;
    public TextMeshProUGUI leftEnemy;

    public TextMeshProUGUI playTime;
    public void Awake()
    {
        
    }

    public void Start()
    {
        
    }

    public void Update()
    {
        UpdateLeftEnemyText();
        playTime.text = $"{Time.timeSinceLevelLoad:F2}";
    }


    public void UpdateLeftEnemyText()
    {
        var count = enemyManager.GetAliveEnemyCount();
        leftEnemy.text = $"Left Enemy: {count}";
    }
}