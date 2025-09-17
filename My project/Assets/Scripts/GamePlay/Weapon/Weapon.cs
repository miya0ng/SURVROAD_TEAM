using UnityEngine;
using static Bullet;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponSO { get; set; }
    public GameObject bulletPrefab;
    private GameObject player;
    [SerializeField] private Transform muzzle;
    public bool IsEquipped { get; private set; }
    private float nextFireTime;

    // private Bullet bullet;
    private TeamId teamId;
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void Equip(LivingEntity owner)
    {
        IsEquipped = true;
        teamId = owner.teamId;
    }

    private void Start()
    {
        teamId = GetComponent<LivingEntity>()?.teamId ?? TeamId.None;
        switch (weaponSO.Type)
        {
            case 1: //long
                break;
            case 2: //short
                break;
            case 3: //install
                break;
            default:
                break;
        }
    }
    void Update()
    {
        if (weaponSO == null)
        {
            Debug.LogError($"{gameObject.name}: WeaponData null");
            return;
        }

        if (muzzle == null || bulletPrefab == null)
        {
            Debug.Log($"{gameObject.name}: muzzle or bullet null");
            return;
        }

        if (!IsEquipped || weaponSO.Type == 3) return;

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
            bullet.SetBullet(player, teamId);
            bulletObj.SetActive(true);
        }
    }
}