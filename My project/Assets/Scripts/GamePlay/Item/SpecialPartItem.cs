using UnityEngine;

public class SpecialPartItem : ItemBase
{
    private GameManager gameManager;

    public override void Start()
    {
        base.Start();
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
    }

    protected override void Collect(GameObject player)
    {
        Debug.Log("Æ¯¼ö ºÎÇ° È¹µæ ¡æ GameManager ÀúÀå");
        gameManager.AddSpecialPart();
        Destroy(gameObject);
    }
}