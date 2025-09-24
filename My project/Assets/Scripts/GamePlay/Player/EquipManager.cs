using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private readonly List<WeaponDriver> equipWeapons = new();
    public IReadOnlyList<WeaponDriver> Slot => equipWeapons;

    private LivingEntity player;

    // Parts Gauge
    [Header("Parts Gauge")]
    [SerializeField] private float parts = 0f;
    public float Parts => parts;
    [SerializeField] private float partsMax = 10f;
    public float PartsMax => partsMax;
    private bool levelUpPending = false;

    // === DTOs ===
    public struct WeaponSlotInfo
    {
        public Sprite Thumbnail;
        public int Level;
        public string Name;
        public bool IsEmpty => Thumbnail == null;
    }

    public struct UpgradeCandidate
    {
        public WeaponSO Weapon;
        public int NextLevel;
        public Sprite Thumbnail;
    }

    // ========= Unity =========
    void Awake()
    {
        player = GetComponentInParent<LivingEntity>();

        var socketObjs = GameObject.FindGameObjectsWithTag("EquipSocket")
            .OrderBy(o => o.name, System.StringComparer.Ordinal).ToArray();
        sockets.Clear();
        foreach (var obj in socketObjs) sockets.Add(obj.transform);

        if (!equipStarterOnStart) return;
        if (starterPrefab == null || starterSO == null || player == null) return;

        EquipWeapon(starterPrefab, starterSO, player);
        RaiseGaugeChanged();
    }

    void Start()
    {

    }

    // =========================================
    // ============== Public API ===============
    // =========================================

    public void AddParts(float amount)
    {
        if (levelUpPending) return;

        parts = Mathf.Clamp(parts + amount, 0f, partsMax);
        RaiseGaugeChanged();

        if (parts >= partsMax)
        {
            levelUpPending = true;
            OnLevelUpReady?.Invoke(); // 팝업 띄우기
        }
    }

    // (A) 팝업에서 “기존 무기 레벨업”을 직접 지정해 호출
    public void ApplyNewEquipExisting(int slotIndex)
    {
        if (!levelUpPending || !IsValidSlotIndex(slotIndex)) return;
        ApplySelectionCore(slotIndex: slotIndex, newPrefab: null, so: null, nextLevel: -1);
    }

    // (B) 팝업에서 “새 무기 장착”을 직접 지정해 호출
    public void ApplyNewEquip(GameObject weaponPrefab, WeaponSO so)
    {
        if (!levelUpPending || weaponPrefab == null || so == null) return;
        ApplySelectionCore(slotIndex: -1, newPrefab: weaponPrefab, so: so, nextLevel: 1);
    }

    // (C) 통합 선택 API (현재 팝업이 사용하는 것)
    public void EquipOrUpgrade(UpgradeCandidate c)
    {
        if (!levelUpPending || c.Weapon == null) return;

        int idx = FindIndexBySO(c.Weapon);
        if (idx >= 0) // 기존 무기 → 레벨업
            ApplySelectionCore(slotIndex: idx, newPrefab: null, so: c.Weapon, nextLevel: c.NextLevel);
        else if (TryGetLevelData(c.Weapon, c.NextLevel, out var data) && data.prefab != null) // 신규 장착
            ApplySelectionCore(slotIndex: -1, newPrefab: data.prefab, so: c.Weapon, nextLevel: c.NextLevel);
    }

    // (D) 외부에서 직접 장착 호출(동일 SO면 자동 레벨업)
    public void EquipWeapon(GameObject weaponPrefab, WeaponSO so, LivingEntity owner)
    {
        int sameIdx = FindIndexBySO(so);
        if (sameIdx >= 0) { LevelUpWeapon(equipWeapons[sameIdx]); return; }

        if (equipWeapons.Count >= maxEquipCount)
        {
            Debug.Log("장착 슬롯 가득 참");
            return;
        }

        var socket = sockets[equipWeapons.Count];
        var driver = CreateWeaponInstance(weaponPrefab, so, 1, socket);

        equipWeapons.Add(driver);
        OnWeaponEquipped?.Invoke(driver);
        OnEquipChanged?.Invoke();
    }

    // 후보 산출
    public List<UpgradeCandidate> GetUpgradeCandidates(int count, WeaponLibrary lib)
    {
        if (lib == null || lib.weapons == null) return new List<UpgradeCandidate>();

        var all = new List<UpgradeCandidate>(lib.weapons.Count);
        foreach (var w in lib.weapons)
            all.Add(BuildCandidate(w, lib));

        // 중복 무기 방지하여 랜덤 픽
        var result = new List<UpgradeCandidate>(count);
        var picked = new HashSet<WeaponSO>();
        int tries = 0, maxTries = all.Count * 3;

        while (result.Count < count && tries++ < maxTries)
        {
            var pick = all[Random.Range(0, all.Count)];
            if (pick.Weapon == null || picked.Contains(pick.Weapon)) continue;
            result.Add(pick);
            picked.Add(pick.Weapon);
        }
        return result;
    }

    public WeaponSlotInfo[] GetSlotInfos()
    {
        return Slot.Select(drv => new WeaponSlotInfo
        {
            Thumbnail = drv?.CurLevelData?.ThumbNail,
            Level = drv?.CurLevel ?? 0,
            Name = drv?.weaponSO?.name ?? string.Empty
        }).ToArray();
    }

    public void ReplaceWeapon(int index, GameObject newWeaponPrefab, WeaponSO so)
    {
        if (!IsValidSlotIndex(index) || newWeaponPrefab == null || so == null) return;

        var old = equipWeapons[index];
        var socket = old.transform.parent;

        var newDriver = CreateWeaponInstance(newWeaponPrefab, so, Mathf.Max(1, old.CurLevel), socket);
        equipWeapons[index] = newDriver;

        if (old) Destroy(old.gameObject);

        OnEquipChanged?.Invoke();
    }

    public void UnEquipWeapon(int index)
    {
        if (!IsValidSlotIndex(index)) return;

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

    // ============= cheat =================
    public void EquipWeapon(WeaponSO so, int level = 1)
    {
        if (!TryGetLevelData(so, level, out var data) || data.prefab == null)
        {
            Debug.LogWarning($"EquipWeapon(so): {so?.Name} Lv{level} 데이터/프리팹 없음");
            return;
        }
        EquipWeapon(data.prefab, so, player);
    }

    public void ForceEquipNew(WeaponSO so, int level = 1)
    {
        bool prevPending = levelUpPending;
        levelUpPending = false;         // 대기 상태 무시
        EquipWeapon(so, level);         // 위 편의 API 재사용
        levelUpPending = prevPending;   // 원복
    }

    // =========================================
    // ============== Internal =================
    // =========================================

    private void ApplySelectionCore(int slotIndex, GameObject newPrefab, WeaponSO so, int nextLevel)
    {
        // slotIndex >= 0 → 기존 무기 레벨업, < 0 → 신규 장착
        if (slotIndex >= 0)
        {
            LevelUpWeapon(equipWeapons[slotIndex]);
        }
        else
        {
            if (newPrefab == null || so == null)
                return;

            // 슬롯 여유 체크
            if (equipWeapons.Count >= maxEquipCount)
            {
                Debug.Log("장착 슬롯 가득 참");
                return;
            }

            var socket = sockets[equipWeapons.Count];
            var driver = CreateWeaponInstance(newPrefab, so, Mathf.Max(1, nextLevel), socket);
            equipWeapons.Add(driver);

            OnWeaponEquipped?.Invoke(driver);
            OnEquipChanged?.Invoke();
        }

        ResetPartsGauge(); // 선택 완료 후 게이지 리셋
    }

    private bool IsValidSlotIndex(int index) => index >= 0 && index < equipWeapons.Count;

    private int FindIndexBySO(WeaponSO so)
        => so == null ? -1 : equipWeapons.FindIndex(w => w && w.weaponSO == so);

    private WeaponDriver CreateWeaponInstance(GameObject prefab, WeaponSO so, int level, Transform socket)
    {
        var obj = Instantiate(prefab, socket);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        var drv = obj.GetComponent<WeaponDriver>();
        drv.Init(player, so, Mathf.Max(1, level));
        return drv;
    }

    private bool TryGetLevelData(WeaponSO so, int level, out WeaponLevelData data)
    {
        data = so?.Levels?.FirstOrDefault(l => l.Level == level);
        return data != null && data.prefab != null;
    }

    private int GetNextLevel(WeaponSO so)
    {
        var eq = Slot.FirstOrDefault(s => s.weaponSO == so);
        return eq != null ? Mathf.Min(eq.CurLevel + 1, 5) : 1;
    }

    private UpgradeCandidate BuildCandidate(WeaponSO so, WeaponLibrary lib)
    {
        if (so == null || lib == null) return default;

        int next = GetNextLevel(so);
        // Levels[0] == Lv1 기준
        var levelData = (so.Levels != null && so.Levels.Count >= next) ? so.Levels[next - 1] : null;
        var thumb = (levelData != null) ? lib.GetThumbnail(levelData.PrefabIndex, next) : null;

        return new UpgradeCandidate { Weapon = so, NextLevel = next, Thumbnail = thumb };
    }

    private void LevelUpWeapon(WeaponDriver drv)
    {
        if (drv == null) return;
        if (drv.SetLevel(drv.CurLevel + 1))
            OnWeaponLeveled?.Invoke(drv);
        OnEquipChanged?.Invoke();
    }

    private void RaiseGaugeChanged() => OnPartsGaugeChanged?.Invoke(parts, partsMax);

    private void ResetPartsGauge()
    {
        levelUpPending = false;
        parts = 0f;
        RaiseGaugeChanged();
    }
}