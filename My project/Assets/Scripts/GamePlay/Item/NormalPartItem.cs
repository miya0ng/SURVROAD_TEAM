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
        base.Start();
        OnTimer = true;
        lifeTimer = 0;
    }

    protected override void Update()
    {
        base.Update();
    }
    protected override void Collect(GameObject player)
    {
        Debug.Log("ItemCollect");
        equip.AddParts(partsValue);
        Destroy(gameObject);
    }
}