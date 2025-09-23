using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class EquipManager : MonoBehaviour
{
    [Header("Starter Loadout")]
    [SerializeField] private bool equipStarterOnStart = true;
    [SerializeField] private GameObject starterPrefab;
    [SerializeField] private WeaponSO starterSO;

    // === Public Events ===
    public event System.Action OnEquipChanged;
    public event System.Action<float, float> OnPartsGaugeChanged;
    public event System.Action OnLevelUpReady;
    public event System.Action<WeaponDriver> OnWeaponLeveled;
    public event System.Action<WeaponDriver> OnWeaponEquipped;
    public event System.Action<WeaponDriver> OnWeaponUnequipped;

    // === State ===
    [SerializeField] private List<Transform> sockets = new();
    [SerializeField] private int maxEquipCount = 3;
    private List<WeaponDriver> equipWeapons = new();
    public IReadOnlyList<WeaponDriver> Slot => equipWeapons;

    private LivingEntity player;

    // Parts Gauge
    [Header("Parts Gauge")]
    [SerializeField] private float parts = 0f;
    public float Parts => parts;
    [SerializeField] private float partsMax = 100f;
    public float PartsMax => partsMax;
    private bool levelUpPending = false;

    void Awake()
    {
        player = GetComponentInParent<LivingEntity>();
        var socketObjs = GameObject.FindGameObjectsWithTag("EquipSocket")
            .OrderBy(o => o.name, System.StringComparer.Ordinal).ToArray();
        sockets.Clear();
        foreach (var obj in socketObjs) sockets.Add(obj.transform);
    }

    private void Start()
    {
        if (!equipStarterOnStart) return;
        if (starterPrefab == null || starterSO == null || player == null) return;

        EquipWeapon(starterPrefab, starterSO, player);
        OnPartsGaugeChanged?.Invoke(parts, partsMax);
    }

    // =========================================
    // ============== Public API ===============
    // =========================================

    public void AddParts(float amount)
    {
        if (levelUpPending) return;

        parts = Mathf.Clamp(parts + amount, 0f, partsMax);
        OnPartsGaugeChanged?.Invoke(parts, partsMax);

        if (parts >= partsMax)
        {
            levelUpPending = true;
            OnLevelUpReady?.Invoke();//pop up 창 띄우기
        }
    }

    public void ApplyLevelUpChoice_LevelUpExisting(int slotIndex)
    {
        if (!levelUpPending) return;
        if (slotIndex < 0 || slotIndex >= equipWeapons.Count) return;

        var driver = equipWeapons[slotIndex];
        var ok = driver.SetLevel(driver.CurLevel + 1);
        if (ok) OnWeaponLeveled?.Invoke(driver);

        ResetPartsGauge();
    }

    public void ApplyLevelUpChoice_EquipNew(GameObject weaponPrefab, WeaponSO so)
    {
        if (!levelUpPending) return;
        EquipWeapon(weaponPrefab, so, player);
        ResetPartsGauge();
    }

    public void EquipWeapon(GameObject weaponPrefab, WeaponSO so, LivingEntity owner)
    {
        // 같은 SO 면
         var same = equipWeapons.FirstOrDefault(w => w != null && w.weaponSO == so);
         if (same != null) { same.SetLevel(same.CurLevel + 1); OnWeaponLeveled?.Invoke(same); OnEquipChanged?.Invoke(); return; }

        if (equipWeapons.Count >= maxEquipCount)
        {
            Debug.Log("장착 슬롯 가득 참");
            return;
        }

        var socket = sockets[equipWeapons.Count];
        var wObj = Instantiate(weaponPrefab, socket);
        wObj.transform.localPosition = Vector3.zero;
        wObj.transform.localRotation = Quaternion.identity;

        var driver = wObj.GetComponent<WeaponDriver>();
        driver.Init(owner, so, 1);

        equipWeapons.Add(driver);

        OnWeaponEquipped?.Invoke(driver);
        OnEquipChanged?.Invoke();
    }

    public void ReplaceWeapon(int index, GameObject newWeaponPrefab, WeaponSO so)
    {
        if (index < 0 || index >= equipWeapons.Count) return;

        var old = equipWeapons[index];
        var socket = old.transform.parent;

        var newObj = Instantiate(newWeaponPrefab, socket);
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;

        var newDriver = newObj.GetComponent<WeaponDriver>();
        newDriver.Init(player, so, Mathf.Max(1, old.CurLevel));

        equipWeapons[index] = newDriver;
        if (old) Destroy(old.gameObject);

        OnEquipChanged?.Invoke();
    }

    public void UnEquipWeapon(int index)
    {
        if (index < 0 || index >= equipWeapons.Count) return;

        var drv = equipWeapons[index];
        equipWeapons.RemoveAt(index);

        OnWeaponUnequipped?.Invoke(drv);
        Destroy(drv.gameObject);

        OnEquipChanged?.Invoke();
        ResetPartsGauge();
    }

    public void UnEquipLast()
    {
        if (equipWeapons.Count == 0) return;
        UnEquipWeapon(equipWeapons.Count - 1);
    }
    // ============= for cheat =================
    public void EquipWeapon(WeaponSO so, int level = 1)
    {
        if (so == null)
        {
            Debug.LogWarning("EquipWeapon(so): SO null");
            return;
        }

        var data = so.Levels.FirstOrDefault(l => l.Level == level);
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning($"EquipWeapon(so): {so.Name} Lv{level} 데이터/프리팹 없음");
            return;
        }

        EquipWeapon(data.prefab, so, player);
    }

    public void ForceEquipNew(WeaponSO so, int level = 1)
    {
        bool prevPending = levelUpPending;
        levelUpPending = false;       // 대기 상태 무시
        EquipWeapon(so, level);       // 위 편의 API 재사용
        levelUpPending = prevPending; // 원복(원하면 그대로 false 유지도 가능)
    }
    // =========================================
    // ============== Internal =================
    // =========================================
    private void ResetPartsGauge()
    {
        levelUpPending = false;
        parts = 0f;
        OnPartsGaugeChanged?.Invoke(parts, partsMax);
    }
}
