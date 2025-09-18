using UnityEngine;
using static Bullet;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponSO;
    public WeaponLevelData CurLevelData { get; private set; }
    private int curLevel = 1; // 현재 무기 레벨

    public GameObject bulletPrefab;
    private GameObject player;
    [SerializeField] private Transform muzzle;

    public bool IsEquipped { get; private set; }
    private float nextFireTime;

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

    public bool SetLevel(int level)
    {
        Debug.Log($"[SetLevel] {weaponSO.Name} 현재: {curLevel}, 시도: {level}");

        var data = weaponSO.Levels.Find(l => l.Level == level);
        curLevel = level;
        CurLevelData = data;

        Debug.Log($"[SetLevel]: 현재: {curLevel} 성공");
        return true;
    }
    public void LevelUp()
    {
        int nextLevel = curLevel + 1;

        var nextData = weaponSO.Levels.Find(l => l.Level == nextLevel);
        if (nextData == null || nextData.prefab == null)
        {
            Debug.Log($"{weaponSO.Name} 최대 레벨 or Prefab 없음");
            return;
        }

        var newObj = Instantiate(nextData.prefab, transform.position, transform.rotation, transform.parent);
        var w = newObj.GetComponent<Weapon>();
        if (w == null)
        {
            Debug.LogError($"{nextData.prefab.name} 에 Weapon 컴포넌트 없음");
            return;
        }

        w.weaponSO = weaponSO;
        w.SetLevel(nextLevel);
        w.Equip(GetComponentInParent<LivingEntity>());

        var equipManager = GetComponentInParent<EquipManager>();
        if (equipManager != null)
        {
            int index = equipManager.Slot.IndexOf(gameObject);
            if (index >= 0)
                equipManager.ReplaceWeapon(index, newObj);
        }

        Debug.Log($"{weaponSO.Name} 레벨업 → Lv.{nextLevel}");
        Destroy(gameObject);
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
        if (weaponSO == null)
        {
            Debug.Log("null");
        }
        return weaponSO.Levels.Find(l => l.Level == curLevel);
    }
}