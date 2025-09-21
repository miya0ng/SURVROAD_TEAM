using UnityEngine;
using static Bullet;
using static UnityEngine.UI.GridLayoutGroup;

public class Weapon : MonoBehaviour
{
    public WeaponSO weaponSO;
    public WeaponLevelData CurLevelData { get; private set; }
    private int curLevel = 1; // 현재 무기 레벨

    public int CurLevel => curLevel;

    public GameObject bulletPrefab;
    private GameObject player;
    [SerializeField] private Transform muzzle;

    [SerializeField] private ParticleSystem fireEffect;
    public bool IsEquipped { get; private set; } = true;
    private float nextFireTime;

    public LayerMask hitLayers;
    public LineRenderer tracerPrefab;
    private float range;

    private TeamId teamId;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        SetLevel(curLevel);
        //range = CurLevelData.AttackRange;
        range = 10f;
    }

    public void Equip(LivingEntity owner)
    {
        IsEquipped = true;
        teamId = owner.teamId;
    }
    public void SetWeaponSO(WeaponSO so)
    {
        weaponSO = so;
        curLevel = 1;
        SetLevel(curLevel);
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

        //var equipManager = GetComponentInParent<EquipManager>();
        var equipManager = player.GetComponentInChildren<EquipManager>();
        if (equipManager != null)
        {
            //int index = equipManager.Slot.IndexOf(gameObject);
            int index = equipManager.IndexOfInternal(gameObject);
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
        //for (int i = 0; i < levelData.ShotCount; i++)
        //{
        //    var bulletObj = Instantiate(levelData.bulletPrefab, muzzle.position, muzzle.rotation);
        //    var bullet = bulletObj.GetComponent<Bullet>();
        //    bullet.SetBullet(player, teamId, levelData);

        //    bulletObj.SetActive(true);
        //}

        //if (levelData.effectPrefab != null)
        //{
        //    Instantiate(levelData.effectPrefab, muzzle.position, muzzle.rotation);
        //}


        for (int i = 0; i < levelData.ShotCount; i++)
        {
            var bulletObj = Instantiate(levelData.bulletPrefab, muzzle.position, muzzle.rotation);
            bulletObj.SetActive(true);
        }    
            Ray ray = new Ray(muzzle.position, muzzle.forward);
        if(Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            // IDamagable에 데미지 전달
            IDamagable target = hit.collider.GetComponent<IDamagable>();
            if (target != null)
                target.OnDamage(levelData.MaxDamage);

            // 이펙트 플레이
            if (fireEffect != null)
                fireEffect.Play();

            SpawnTracer(muzzle.position, hit.point);
        }
        else
        {
            if (fireEffect != null)
                fireEffect.Play();

            SpawnTracer(muzzle.position, muzzle.position + muzzle.forward * range);
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

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        var tracer = Instantiate(tracerPrefab);
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        Destroy(tracer.gameObject, 0.1f); // 아주 짧게만 유지

        var bulletMesh = Instantiate(bulletPrefab, start, Quaternion.identity);
        var bullet = bulletMesh.GetComponent<Bullet>();
        bulletMesh.transform.position = start;
    }
}