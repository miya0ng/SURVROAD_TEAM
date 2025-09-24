using UnityEngine;

public class NormalPartItem : ItemBase
{
    private EquipManager equip;

    public float partsValue = 10f;

    public void Awake()
    {
        equip = GameObject.FindWithTag("EquipManager").GetComponent<EquipManager>();
    }
    public override void Start()
    {
    }    
    protected override void Collect(GameObject player)
    {
        equip.AddParts(partsValue);
        Destroy(gameObject);
    }
}