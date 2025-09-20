using UnityEngine;

public class DestructibleObject : LivingEntity
{
    public GameObject[] dropItems;
    public float[] dropRates;

    private ItemManager itemManager;

    public void Start()
    {
        itemManager = GameObject.FindWithTag("ItemManager").GetComponent<ItemManager>();
    }
    protected override void Die()
    {
        base.Die();
        OnBreak();
        // Æø¹ß È¿°ú
        // Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void OnBreak()
    {
        itemManager.DropFromObject(transform.position);
    }
}