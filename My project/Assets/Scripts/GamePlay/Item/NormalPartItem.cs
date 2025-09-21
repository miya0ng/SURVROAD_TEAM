using UnityEngine;

public class NormalPartItem : ItemBase
{
    private Ui_Slider slider;

    public void Awake()
    {
        slider = GameObject.FindWithTag("PartsGuage").GetComponent<Ui_Slider>();
    }
    public override void Start()
    {
    }    
    protected override void Collect(GameObject player)
    {
        slider.UpdatePartsSlider(partsValue);
        Destroy(gameObject);
    }
}