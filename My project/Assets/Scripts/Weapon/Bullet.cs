using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Bullet : MonoBehaviour
{
    public WeaponData weaponData;

    private GameObject owner;
    public enum TeamId
    {
        None,
        Player,
        Enemy
    }

    public TeamId teamId;

    private float timer;
    private Rigidbody rb;
    private HashSet<GameObject> hitTargets = new();
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void OnEnable()
    {
        timer = 0f;
        hitTargets.Clear();
        rb.linearVelocity = transform.forward * weaponData.bulletSpeed;
    }

    public void SetBullet(GameObject owner, TeamId team)
    {
        this.owner = owner;

        var bulletCol = GetComponent<Collider>();
        var ownerCol = owner.GetComponent<Collider>();
        if (bulletCol && ownerCol)
            Physics.IgnoreCollision(bulletCol, ownerCol);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= weaponData.lifeTime)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var target = other.GetComponent<IDamagable>();
        Debug.Log($"=================Bullet hit================");

        if (other.gameObject == owner) return;

        var livingEntity = other.GetComponent<LivingEntity>();
        if (livingEntity != null && livingEntity.teamId == teamId)
            return; // 같은 팀이면 데미지 무시


        if (target != null && !hitTargets.Contains(other.gameObject))
        {
            Debug.Log($"Bullet hit {other.gameObject.name}");
            target.OnDamage(weaponData.damage, null);
            hitTargets.Add(other.gameObject);
        }

        gameObject.SetActive(false);
    }
}