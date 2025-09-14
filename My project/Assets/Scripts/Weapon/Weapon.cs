using UnityEngine;
using static Bullet;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon : MonoBehaviour
{
    public WeaponData weaponData;
    [SerializeField] private Transform muzzle;

    private float nextFireTime;
    private Bullet bullet;
    private TeamId teamId;
    private void Awake()
    {
       
    }

    private void Start()
    {
        teamId = GetComponent<LivingEntity>()?.teamId ?? TeamId.None;
    }
    void Update()
    {
        if (weaponData == null || muzzle == null)
        {
            Debug.LogError("WeaponData ¶Ç´Â MuzzleÀÌ null");
            return;
        }

        nextFireTime += Time.deltaTime;

        if (nextFireTime >= weaponData.attackSpeed)
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
            bullet.SetBullet(gameObject, teamId);
            bulletObj.SetActive(true);
        }
    }
}
