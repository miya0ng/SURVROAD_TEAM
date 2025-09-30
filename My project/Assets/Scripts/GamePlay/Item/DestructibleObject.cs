using System.Collections;
using UnityEngine;

public class DestructibleObject : LivingEntity
{
    public GameObject[] dropItems;
    public ParticleSystem explosionPrefab;
    public float[] dropRates;
    private ItemManager itemManager;
    private HitFlash hitFlash;
    protected override void Awake()
    {
        base.Awake();
        maxHp = 20;
    }
    public void Start()
    {
        itemManager = GameObject.FindWithTag("ItemManager").GetComponent<ItemManager>();
        hitFlash = GetComponent<HitFlash>();
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        hitFlash.PlayFlash();
    }
    protected override void Die(LivingEntity killer)
    {
        base.Die();
        OnBreak();

        var explosion = Instantiate(explosionPrefab, transform.position, explosionPrefab.transform.rotation);
        explosion.Play();

        float totalDuration = explosion.main.duration + explosion.main.startLifetime.constantMax;

        Destroy(explosion.gameObject, totalDuration);

        Destroy(gameObject);
    }

    public void OnBreak()
    {
        var posY = transform.position.y + 2f;
        var pos = new Vector3(transform.position.x, posY, transform.position.z);
        itemManager.DropFromObject(pos);
    }
}