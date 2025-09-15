using UnityEngine;
using static Bullet;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponSO;
    public GameObject bulletPrefab;
    [SerializeField] private Transform muzzle;

    private float nextFireTime;
   // private Bullet bullet;
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
        if (weaponSO == null || muzzle == null)
        {
            Debug.LogError("WeaponData ¶Ç´Â MuzzleÀÌ null");
            return;
        }

        nextFireTime += Time.deltaTime;

        if (nextFireTime >= weaponSO.AttackSpeed)
        {
            Fire();
            nextFireTime = 0f;
        }
    }

    void Fire()
    {
        for (int i = 0; i < weaponSO.ShotCount; i++)
        {
            var bulletObj = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
            var bullet = bulletObj.GetComponent<Bullet>();
            bullet.weaponSO = weaponSO;
            bullet.SetBullet(gameObject, teamId);
            bulletObj.SetActive(true);
        }
    }
}
