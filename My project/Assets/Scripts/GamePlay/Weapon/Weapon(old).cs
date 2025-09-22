//using UnityEngine;
//using static Bullet;
//using static UnityEngine.UI.GridLayoutGroup;

//public class Weapon : MonoBehaviour
//{
//    private EnemySpawner enemySpawner;
    
//    public WeaponSO weaponSO;
//    public WeaponLevelData CurLevelData { get; private set; }
//    private int curLevel = 1; // 현재 무기 레벨

//    public int CurLevel => curLevel;

//    public GameObject bulletPrefab;
//    private GameObject player;
//    private LivingEntity ownerEntity;
//    [SerializeField] private Transform muzzle;

//    [SerializeField] private ParticleSystem fireEffect;
//    public bool IsEquipped { get; private set; } = true;
//    private float nextFireTime;

//    private TeamId teamId;

//    private void Awake()
//    {
//        ownerEntity = GetComponentInParent<LivingEntity>();
//        enemySpawner = GameObject.FindWithTag("EnemySpawner").GetComponent<EnemySpawner>();
//        SetLevel(curLevel);
//    }

//    private void Start()
//    {
//        teamId = ownerEntity.teamId;

//        switch (weaponSO.Type)
//        {
//            case 1: // long
//                break;
//            case 2: // short
//                break;
//            case 3: // install
//                break;
//            default:
//                break;
//        }
//    }
//    public void SetWeaponSO(WeaponSO so)
//    {
//        weaponSO = so;
//        curLevel = 1;
//        SetLevel(curLevel);
//    }
//    public bool SetLevel(int level)
//    {
//        Debug.Log($"[SetLevel] {weaponSO.Name} 현재: {curLevel}, 시도: {level}");

//        var data = weaponSO.Levels.Find(l => l.Level == level);
//        curLevel = level;
//        CurLevelData = data;

//        Debug.Log($"[SetLevel]: 현재: {curLevel} 성공");
//        return true;
//    }
//    public void LevelUp()
//    {
//        int nextLevel = curLevel + 1;

//        var nextData = weaponSO.Levels.Find(l => l.Level == nextLevel);
//        if (nextData == null || nextData.prefab == null)
//        {
//            Debug.Log($"{weaponSO.Name} 최대 레벨 or Prefab 없음");
//            return;
//        }

//        var newObj = Instantiate(nextData.prefab, transform.position, transform.rotation, transform.parent);
//        var w = newObj.GetComponent<Weapon>();
//        if (w == null)
//        {
//            Debug.LogError($"{nextData.prefab.name} 에 Weapon 컴포넌트 없음");
//            return;
//        }

//        w.weaponSO = weaponSO;
//        w.SetLevel(nextLevel);

//        var equipManager = player.GetComponentInChildren<EquipManager>();
//        if (equipManager != null)
//        {
//            int index = equipManager.IndexOfInternal(gameObject);
//            if (index >= 0)
//                equipManager.ReplaceWeapon(index, newObj);
//        }

//        Debug.Log($"{weaponSO.Name} 레벨업 → Lv.{nextLevel}");
//        Destroy(gameObject);
//    }


//    void Update()
//    {
//        if (weaponSO == null)
//        {
//            Debug.LogError($"{gameObject.name}: WeaponSO null");
//            return;
//        }

//        if (muzzle == null)
//        {
//            Debug.LogWarning($"{gameObject.name}: muzzle null");
//            return;
//        }

//        if (!IsEquipped || weaponSO.Type == 3) return;

//        var levelData = GetCurrentLevelData();
//        if (levelData == null) return;

//        nextFireTime += Time.deltaTime;

//        if (nextFireTime >= levelData.AttackSpeed)
//        {
//            AimAtClosestEnemy();
//            Fire(levelData);
//            nextFireTime = 0f;
//        }
//    }

//    void Fire(WeaponLevelData levelData)
//    {
//        for (int i = 0; i < levelData.ShotCount; i++)
//        {

//            var bulletObj = Instantiate(levelData.bulletPrefab, muzzle.position, muzzle.rotation);
//            //levelData.Duration
//            bulletObj.GetComponent<Bullet>().Init(levelData.BulletSpeed,3, levelData.MaxDamage, teamId, ownerEntity);
//            bulletObj.SetActive(true);
//            if (fireEffect != null)
//                fireEffect.Play();
//        }
//    }

//    private WeaponLevelData GetCurrentLevelData()
//    {
//        if (weaponSO == null)
//        {
//            Debug.Log("null");
//        }
//        return weaponSO.Levels.Find(l => l.Level == curLevel);
//    }

//    private void AimAtClosestEnemy()
//    {
//        var target = FindClosestEnemy();
//        if (target == null) return;

//        Vector3 dir = (target.transform.position - transform.position).normalized;

//        dir.y = 0f;

//        if (dir.sqrMagnitude > 0.001f)
//        {
//            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
//            transform.rotation = targetRot;
//        }
//    }

//    private LivingEntity FindClosestEnemy()
//    {
//        var enemies = enemySpawner.GetEnemies();
//        LivingEntity closest = null;
//        float closestDist = float.MaxValue;

//        foreach (var e in enemies)
//        {
//            if (!e.gameObject.activeInHierarchy) continue;
//            float dist = Vector3.Distance(transform.position, e.transform.position);
//            if (dist < closestDist)
//            {
//                closestDist = dist;
//                closest = e;
//            }
//        }
//        return closest;
//    }
//}