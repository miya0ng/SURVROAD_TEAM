using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform muzzle;

    private float nextFireTime;


    void Update()
    {
        nextFireTime += Time.deltaTime;
        if (nextFireTime == weaponData.attackSpeed)
        {
            Fire();
            nextFireTime = 0f;
        }
    }

    void Fire()
    {
        for (int i = 0; i < weaponData.bulletCount; i++)
        {
            var bulletObj = Instantiate(weaponData.bulletPrefab, muzzle.position, muzzle.rotation);
            var bullet = bulletObj.GetComponent<Bullet>();
            bullet.weaponData = weaponData;
        }
    }
}
