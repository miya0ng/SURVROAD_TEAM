using System.Collections;
using UnityEngine;

public class DestructibleObject : LivingEntity
{
    public GameObject[] dropItems;
    public ParticleSystem explosionPrefab;
    public float[] dropRates;
    private ItemManager itemManager;
    private HitFlash hitFlash;
    public void Awake()
    {
        maxHp = 20;
        curHp = maxHp;
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
    protected override void Die()
    {
        base.Die();
        OnBreak();

        // 폭발 프리팹을 따로 Instantiate 해서 재생
        var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.Play();

        // 전체 재생 시간 계산
        float totalDuration = explosion.main.duration + explosion.main.startLifetime.constantMax;

        // 재생 끝나면 폭발 이펙트 파괴
        Destroy(explosion.gameObject, totalDuration);

        // 자기 자신도 제거
        Destroy(gameObject);
    }

    public void OnBreak()
    {
        itemManager.DropFromObject(transform.position);
    }
}