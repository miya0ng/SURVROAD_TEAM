using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Ui_Game : MonoBehaviour
{
    public EnemyManager enemyManager;
    public Slider hpBar;
    public TextMeshProUGUI leftEnemy;
    private TextMeshProUGUI hpText;
    public void Awake()
    {
        hpText = hpBar.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Update()
    {
        UpdateLeftEnemyText();
    }
    public void SetHpBar(float MaxHp)
    {
        hpBar.maxValue = MaxHp;
        hpText.text = $"{MaxHp} <#ffc9d6> / {MaxHp}";
    }
    public void UpdateHpBar(float curHp)
    {
        hpBar.value = curHp;
        hpText.text = $"{curHp} <#ffc9d6> / {hpBar.maxValue}";
    }

    public void UpdateLeftEnemyText()
    {
        var count = enemyManager.GetAliveEnemyCount();
        leftEnemy.text = $"Left Enemy: {count}";
    }
}