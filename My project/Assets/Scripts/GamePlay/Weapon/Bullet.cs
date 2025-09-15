using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Bullet : MonoBehaviour
{
    public WeaponSO weaponSO;

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
        rb.linearVelocity = transform.forward * weaponSO.BulletSpeed;
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
        if (timer >= weaponSO.Duration)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var target = other.GetComponent<IDamagable>();

        if (other.gameObject == owner) return;

        var livingEntity = other.GetComponent<LivingEntity>();
        if (livingEntity != null && livingEntity.teamId == teamId)
            return;

        if (target != null && !hitTargets.Contains(other.gameObject))
        {
            Debug.Log($"Bullet hit {other.gameObject.name}");
            target.OnDamage(weaponSO.Damage, null);
            hitTargets.Add(other.gameObject);
            gameObject.SetActive(false);
        }
    }
}