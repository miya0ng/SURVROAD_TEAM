using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Ui_Game : MonoBehaviour
{
    public Slider hpBar;
    private TextMeshProUGUI hpText;
    public void Awake()
    {
        hpText = hpBar.GetComponentInChildren<TextMeshProUGUI>();
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
}