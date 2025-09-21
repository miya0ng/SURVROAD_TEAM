using UnityEngine;

public class DestructibleObject : LivingEntity
{
    public GameObject[] dropItems;
    public float[] dropRates;
    private ItemManager itemManager;

    public void Awake()
    {
        maxHp = 20;
        curHp = maxHp;
    }
    public void Start()
    {
        itemManager = GameObject.FindWithTag("ItemManager").GetComponent<ItemManager>();
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
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