using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Ui_Slider : MonoBehaviour
{
    private TextMeshProUGUI Slider;
    public Slider slider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetSliderUi(float curValue, float maxValue)
    {
        slider.maxValue = maxValue;
        slider.value = curValue;
        Slider = slider.GetComponentInChildren<TextMeshProUGUI>();
        Slider.text = $"{curValue} <#ffc9d6> / {maxValue}";
    }
    public void UpdateHpSlider(float curValue)
    {
        slider.value = curValue;
        Slider.text = $"{curValue} <#ffc9d6> / {slider.maxValue}";
    }
    public void UpdatePartsSlider(float amount)
    {
        slider.value += amount;
        Slider.text = $"{slider.value} <#ffc9d6> / {slider.maxValue}";
    }
}