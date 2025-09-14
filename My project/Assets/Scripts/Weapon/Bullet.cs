using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public WeaponData weaponData; // ���� ������ ���� (Weapon���� ����)

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

        if (target != null && !hitTargets.Contains(other.gameObject))
        {
            target.OnDamage(weaponData.damage, null);
            hitTargets.Add(other.gameObject);
        }

        gameObject.SetActive(false);
    }
}
