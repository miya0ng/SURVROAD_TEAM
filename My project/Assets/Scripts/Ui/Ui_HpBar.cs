using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Ui_HpBar : MonoBehaviour
{
    private TextMeshProUGUI hpText;
    public Slider hpBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetHpBar(float MaxHp)
    {
        hpBar.maxValue = MaxHp;
        hpBar.value = MaxHp;
        hpText = hpBar.GetComponentInChildren<TextMeshProUGUI>();
        hpText.text = $"{MaxHp} <#ffc9d6> / {MaxHp}";
    }
    public void UpdateHpBar(float curHp)
    {
        hpBar.value = curHp;
        hpText.text = $"{curHp} <#ffc9d6> / {hpBar.maxValue}";
    }
}