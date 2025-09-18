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

    public int curLevel = 1; // 현재 무기 레벨
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

        // 타입 분류 예시
        switch (weaponSO.Type)
        {
            case 1: // long
                break;
            case 2: // short
                break;
            case 3: // install
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if (weaponSO == null)
        {
            Debug.LogError($"{gameObject.name}: WeaponSO null");
            return;
        }

        if (muzzle == null)
        {
            Debug.LogWarning($"{gameObject.name}: muzzle null");
            return;
        }

        if (!IsEquipped || weaponSO.Type == 3) return;

        var levelData = GetCurrentLevelData();
        if (levelData == null) return;

        nextFireTime += Time.deltaTime;

        if (nextFireTime >= levelData.AttackSpeed)
        {
            Fire(levelData);
            nextFireTime = 0f;
        }
    }

    void Fire(WeaponLevelData levelData)
    {
        for (int i = 0; i < levelData.ShotCount; i++)
        {
            var bulletObj = Instantiate(levelData.bulletPrefab, muzzle.position, muzzle.rotation);
            var bullet = bulletObj.GetComponent<Bullet>();

            bullet.SetBullet(player, teamId, levelData);

            bulletObj.SetActive(true);
        }

        if (levelData.effectPrefab != null)
        {
            Instantiate(levelData.effectPrefab, muzzle.position, muzzle.rotation);
        }
    }

    private WeaponLevelData GetCurrentLevelData()
    {
        return weaponSO.Levels.Find(l => l.Level == curLevel);
    }
}
