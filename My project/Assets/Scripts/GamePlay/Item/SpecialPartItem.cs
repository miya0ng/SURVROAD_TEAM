using UnityEngine;

public class SpecialPartItem : ItemBase
{
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
    }

    protected override void Collect(GameObject player)
    {
        Debug.Log("Ư�� ��ǰ ȹ�� �� GameManager ����");
        gameManager.AddSpecialPart();
        Destroy(gameObject);
    }
}