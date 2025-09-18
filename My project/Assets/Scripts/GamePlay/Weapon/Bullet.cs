using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private WeaponLevelData levelData;

    private GameObject owner;
    public enum TeamId { None, Player, Enemy }
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

        if (levelData != null && rb != null)
        {
            rb.linearVelocity = transform.forward * levelData.BulletSpeed;
        }
    }

    public void SetBullet(GameObject owner, TeamId team, WeaponLevelData data)
    {
        this.owner = owner;
        this.teamId = team;
        this.levelData = data;

        var bulletCol = GetComponent<Collider>();
        var ownerCol = owner.GetComponent<Collider>();
        if (bulletCol && ownerCol)
            Physics.IgnoreCollision(bulletCol, ownerCol);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (levelData != null && timer >= levelData.Duration)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        var livingEntity = other.GetComponent<LivingEntity>();
        if (livingEntity != null && livingEntity.teamId == teamId)
            return;

        var target = other.GetComponent<IDamagable>();
        if (target != null && !hitTargets.Contains(other.gameObject))
        {
            float damage = Random.Range(levelData.MinDamage, levelData.MaxDamage);
            Debug.Log($"Bullet hit {other.gameObject.name} �� {damage} damage");

            target.OnDamage(damage, null);
            hitTargets.Add(other.gameObject);

            gameObject.SetActive(false);
        }
    }
}
