using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipManager : MonoBehaviour
{
    // ====== Starter Loadout ======
    [Header("Starter Loadout")]
    [SerializeField] private bool equipStarterOnStart = true;
    [SerializeField] private GameObject starterPrefab;
    [SerializeField] private WeaponSO starterSO;

    // ====== Events ======
    public event System.Action OnEquipChanged;
    public event System.Action OnCandidate;
    //public event System.Action OnPartsCandidate;
    public event System.Action<float, float> OnPartsGaugeChanged;
    public event System.Action OnLevelUpReady;
    public event System.Action<WeaponDriver> OnWeaponLeveled;
    public event System.Action<WeaponDriver> OnWeaponEquipped;
    public event System.Action<WeaponDriver> OnWeaponUnequipped;
    public event System.Action<PlayerUpgradeOption> OnPlayerUpgrade;

    // ====== State ======
    [SerializeField] private int maxEquipCount = 3;

    private LivingEntity player;

    // 슬롯(논리 장착): driver가 null이면 "가상 장착"
    private class EquippedEntry
    {
        public WeaponSO so;
        public int level;
        public WeaponDriver driver;           // 대표 드라이버(하위 호환)
        public List<WeaponDriver> drivers = new(); // 모든 인스턴스(좌/우 등)
        public List<EquipSocket> sockets;     // 점유 소켓들(NonOccupying은 null/빈)
        public bool IsMounted => driver != null;
    }

    private readonly List<EquippedEntry> equips = new();
    public int EquippedCount => equips.Count;

    // 하위 호환: 기존 entity 배열을 노출(가상 슬롯은 null)
    public IReadOnlyList<WeaponDriver> Slot => equips.Select(e => e.driver).ToList();

    // 소켓 맵
    private readonly Dictionary<SocketType, List<EquipSocket>> socketMap = new();

    // ====== Parts Gauge ======
    [Header("Parts Gauge")]
    [SerializeField] private float parts = 0f;
    public float Parts => parts;
    [SerializeField] private float partsMax = 50f;
    public float PartsMax => partsMax;
    private bool levelUpPending = false;

    private Ui_Slider partsGaugeUi;

    // ====== PlayerUpgrade ======
    [Header("Player Upgrade Icons")]
    [SerializeField] private Sprite iconDurability;
    [SerializeField] private Sprite iconMaxSpeed;
    [SerializeField] private Sprite iconAcceleration;

    // 누적 멀티플라이어(기본 1.0)
    private float durabilityMul = 1f;
    private float maxSpeedMul = 1f;
    private float accelerationMul = 1f;


    // ========= Unity =========
    void Awake()
    {
        player = GetComponentInParent<LivingEntity>();
        partsGaugeUi = GameObject.FindWithTag("PartsGuage")?.GetComponent<Ui_Slider>();
        // 소켓 수집
        socketMap.Clear();
        var sockets = player ? player.GetComponentsInChildren<EquipSocket>()
                             : GetComponentsInChildren<EquipSocket>();
        foreach (var s in sockets)
        {
            if (!socketMap.TryGetValue(s.type, out var list))
            {
                socketMap[s.type] = list = new();
            }
            list.Add(s);
        }

        // 스타터 장착
        if (equipStarterOnStart && starterSO != null && player != null)
        {
            EquipWeapon(starterSO, 1);
        }
        //else if (equipStarterOnStart && starterPrefab != null && starterSO != null && player != null)
        //{
        //    EquipWeapon(starterPrefab, starterSO, player);
        //}

        RaiseGaugeChanged();
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

    public void ApplyNewEquipExisting(int slotIndex)
    {
        if (!levelUpPending || !IsValidSlotIndex(slotIndex)) return;

        var entry = equips[slotIndex];
        if (entry.level >= 5) { Debug.Log("이미 최대 레벨입니다."); return; }

        entry.level = Mathf.Min(entry.level + 1, 5);

        if (entry.IsMounted)
        {
            // 기존 드라이버 제거
            foreach (var d in entry.drivers)
            {
                if (d) Destroy(d.gameObject);
            }
            entry.drivers.Clear();

            ReleaseSockets(entry);

            if (TryGetLevelData(entry.so, entry.level, out var newData) && newData.prefab != null)
            {
                if (TryAssignMount(entry.so, out var newSockets))
                {
                    var created = CreateDriversByPolicy(entry.so, entry.level, newSockets, out var socketsUsed);
                    entry.drivers = created;
                    entry.driver = created.FirstOrDefault();
                    entry.sockets = socketsUsed;

                    foreach (var d in created)
                        OnWeaponLeveled?.Invoke(d);
                }
                else
                {
                    Debug.LogWarning($"[EquipManager] 레벨업 실패: 소켓을 찾을 수 없습니다 - {entry.so.Name}");
                    entry.driver = null;
                    entry.drivers.Clear();
                    entry.sockets = null;
                }
            }
        }
        else
        {
            entry.driver = null;
        }

        OnEquipChanged?.Invoke();
        ResetPartsGauge();
    }

    //public void ApplyNewEquip(GameObject weaponPrefab, WeaponSO so)
    //{
    //    if (!levelUpPending || weaponPrefab == null || so == null) return;
    //    EquipNewCore(so, 1, preferPhysical: true);
    //    ResetPartsGauge();
    //}

    public void EquipOrUpgrade(UpgradeCandidate c)
    {
        if (!levelUpPending) return;
        OnCandidate?.Invoke();
        ChangePartsGuage();

        if (c.Kind == CandidateKind.PlayerStat)
        {
            ApplyPlayerUpgrade(c.PlayerUpgrade);
            ResetPartsGauge();
            return;
        }

        if (c.Weapon == null) return;

        int idx = FindIndexBySO(c.Weapon);
        if (idx >= 0)
        {
            ApplyNewEquipExisting(idx);
        }
        else
        {
            EquipNewCore(c.Weapon, Mathf.Max(1, c.NextLevel), preferPhysical: true);
            ResetPartsGauge();
        }
    }

    //public void EquipWeapon(GameObject weaponPrefab, WeaponSO so, LivingEntity owner)
    //{
    //    int sameIdx = FindIndexBySO(so);
    //    if (sameIdx >= 0) { ApplyNewEquipExisting(sameIdx); return; }

    //    if (!TryGetLevelData(so, 1, out var data) || data.prefab == null) return;
    //    TryMountOrVirtual(so, 1, preferPhysical: true);
    //}

    public void EquipWeapon(WeaponSO so, int level = 1)
    {
        int sameIdx = FindIndexBySO(so);
        if (sameIdx >= 0)
        {
            var e = equips[sameIdx];
            if (e.level >= 5) return;
            e.level = Mathf.Min(e.level + 1, 5);
            SyncMountedLevel(sameIdx);
            OnEquipChanged?.Invoke();
            return;
        }
        TryMountOrVirtual(so, level, preferPhysical: true);
    }

    public WeaponSlotInfo[] GetSlotInfos()
    {
        var list = new List<WeaponSlotInfo>(maxEquipCount);
        for (int i = 0; i < maxEquipCount; i++)
        {
            if (i >= equips.Count || equips[i] == null)
            {
                list.Add(new WeaponSlotInfo());
                continue;
            }

            var e = equips[i];
            Sprite thumb = null;
            string name = e.so ? e.so.Name : string.Empty;

            if (e.driver != null && e.driver.CurLevelData != null)
                thumb = e.driver.CurLevelData.ThumbNail;
            else if (e.so != null)
            {
                var lvData = e.so.Levels.FirstOrDefault(l => l.Level == e.level);
                if (lvData != null) thumb = lvData.ThumbNail;
            }

            list.Add(new WeaponSlotInfo
            {
                Thumbnail = thumb,
                Level = e.level,
                Name = name
            });
        }
        return list.ToArray();
    }

    //public void ReplaceWeapon(int index, GameObject newWeaponPrefab, WeaponSO so)
    //{
    //    if (!IsValidSlotIndex(index) || newWeaponPrefab == null || so == null) return;

    //    var entry = equips[index];

    //    if (entry.drivers != null)
    //    {
    //        foreach (var d in entry.drivers)
    //        {
    //            if (d) OnWeaponUnequipped?.Invoke(d);
    //            if (d) Destroy(d.gameObject);
    //        }
    //        entry.drivers.Clear();
    //    }
    //    entry.driver = null;
    //    ReleaseSockets(entry);

    //    entry.so = so;
    //    entry.level = Mathf.Max(1, entry.level);

    //    TryMountIntoExistingEntry(entry, preferPhysical: true);
    //    equips[index] = entry;
    //    OnEquipChanged?.Invoke();
    //}

    //public void UnEquipWeapon(int index)
    //{
    //    if (!IsValidSlotIndex(index)) return;

    //    var entry = equips[index];

    //    if (entry.drivers != null)
    //    {
    //        foreach (var d in entry.drivers)
    //        {
    //            if (d) OnWeaponUnequipped?.Invoke(d);
    //            if (d) Destroy(d.gameObject);
    //        }
    //    }

    //    ReleaseSockets(entry);
    //    equips.RemoveAt(index);
    //    OnEquipChanged?.Invoke();
    //    ResetPartsGauge();
    //}

    //public void UnEquipLast()
    //{
    //    if (equips.Count == 0) return;
    //    UnEquipWeapon(equips.Count - 1);
    //}

    //// ============= cheat =================
    //public void ForceEquipNew(WeaponSO so, int level = 1)
    //{
    //    bool prevPending = levelUpPending;
    //    levelUpPending = false;
    //    EquipWeapon(so, level);
    //    levelUpPending = prevPending;
    //}

    //public void TryMaterializeMounts()
    //{
    //    foreach (var e in equips.Where(x => !x.IsMounted).ToList())
    //        TryMountIntoExistingEntry(e, preferPhysical: true);
    //    OnEquipChanged?.Invoke();
    //}

    // =========================================
    // ============== Internal =================
    // =========================================
    private void EquipNewCore(WeaponSO so, int level, bool preferPhysical)
    {
        if (equips.Count >= maxEquipCount)
        {
            Debug.Log("장착 슬롯 가득 참");
            return;
        }
        TryMountOrVirtual(so, level, preferPhysical);
    }

    private void TryMountOrVirtual(WeaponSO so, int level, bool preferPhysical)
    {
        var entry = new EquippedEntry { so = so, level = Mathf.Max(1, level) };

        if (preferPhysical && TryAssignMount(so, out var chosen))
        {
            var created = CreateDriversByPolicy(so, entry.level, chosen, out var socketsUsed);
            entry.drivers = created;
            entry.driver = created.FirstOrDefault();
            entry.sockets = socketsUsed;

            foreach (var d in created) OnWeaponEquipped?.Invoke(d);
        }
        else
        {
            entry.driver = null;
            entry.drivers.Clear();
            entry.sockets = null;
        }

        equips.Add(entry);
        OnEquipChanged?.Invoke();
    }

    //private void TryMountIntoExistingEntry(EquippedEntry entry, bool preferPhysical)
    //{
    //    if (!preferPhysical || entry.so == null) return;

    //    if (TryAssignMount(entry.so, out var chosen))
    //    {
    //        var created = CreateDriversByPolicy(entry.so, entry.level, chosen, out var socketsUsed);
    //        entry.drivers = created;
    //        entry.driver = created.FirstOrDefault();
    //        entry.sockets = socketsUsed;

    //        foreach (var d in created) OnWeaponEquipped?.Invoke(d);
    //    }
    //    else
    //    {
    //        entry.driver = null;
    //        entry.drivers.Clear();
    //        entry.sockets = null;
    //    }
    //}

    private List<WeaponDriver> CreateDriversByPolicy(WeaponSO so, int level, List<EquipSocket> chosen, out List<EquipSocket> socketsUsed)
    {
        socketsUsed = new List<EquipSocket>();

        if (!TryGetLevelData(so, level, out var data) || data.prefab == null)
            return new List<WeaponDriver>();

        switch (so.mountPolicy)
        {
            case MountPolicy.Single:
                {
                    var sock = chosen[0];
                    var parent = (sock != null) ? (sock.soket ? sock.soket : sock.transform) : transform;
                    var drv = CreateWeaponInstance(data.prefab, so, level, parent);
                    if (sock != null) { sock.occupied = true; socketsUsed.Add(sock); }
                    return new List<WeaponDriver> { drv };
                }
            case MountPolicy.PairSymmetric:
                {
                    var left = chosen[0]; var right = chosen[1];
                    var lt = left.soket ? left.soket : left.transform;
                    var rt = right.soket ? right.soket : right.transform;

                    var drvL = CreateWeaponInstance(data.prefab, so, level, lt);
                    var drvR = CreateWeaponInstance(data.prefab, so, level, rt);

                    left.occupied = right.occupied = true;
                    socketsUsed.Add(left);
                    socketsUsed.Add(right);

                    return new List<WeaponDriver> { drvL, drvR };
                }
            case MountPolicy.NonOccupying:
                {
                    var anchor = chosen[0];
                    var parent = (anchor != null) ? (anchor.soket ? anchor.soket : anchor.transform) : transform;
                    var drv = CreateWeaponInstance(data.prefab, so, level, parent);
                    return new List<WeaponDriver> { drv };
                }
        }
        return new List<WeaponDriver>();
    }

    private void ReleaseSockets(EquippedEntry entry)
    {
        if (entry.sockets != null)
        {
            foreach (var s in entry.sockets)
                if (s) s.occupied = false;
            entry.sockets.Clear();
        }
    }

    private void SyncMountedLevel(int index)
    {
        var e = equips[index];
        if (e.drivers != null && e.drivers.Count > 0)
        {
            foreach (var d in e.drivers)
            {
                if (d) Destroy(d.gameObject);
            }
            e.drivers.Clear();

            ReleaseSockets(e);

            if (TryGetLevelData(e.so, e.level, out var newData) && newData.prefab != null)
            {
                if (TryAssignMount(e.so, out var newSockets))
                {
                    var created = CreateDriversByPolicy(e.so, e.level, newSockets, out var socketsUsed);
                    e.drivers = created;
                    e.driver = created.FirstOrDefault();
                    e.sockets = socketsUsed;

                    foreach (var d in created)
                        OnWeaponLeveled?.Invoke(d);
                }
                else
                {
                    Debug.LogWarning($"[EquipManager] SyncLevel 실패: 소켓 부족 - {e.so.Name}");
                }
            }
        }
    }

    private bool IsValidSlotIndex(int index) => index >= 0 && index < equips.Count;

    private int FindIndexBySO(WeaponSO so)
        => so == null ? -1 : equips.FindIndex(w => w != null && w.so == so);

    private WeaponDriver CreateWeaponInstance(GameObject prefab, WeaponSO so, int level, Transform parent)
    {
        Debug.Log($"[Equip] '{prefab.name}' parent={parent?.name}");

        var obj = Instantiate(prefab, parent);
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

    // ========== 소켓 배정 ==========
    private bool TryAssignMount(WeaponSO so, out List<EquipSocket> chosen)
    {
        chosen = null;

        IEnumerable<SocketType> order = (so.PreferenceOrder != null && so.PreferenceOrder.Length > 0)
            ? so.PreferenceOrder
            : System.Enum.GetValues(typeof(SocketType)).Cast<SocketType>()
              .Where(t => ((int)so.Allowed & (1 << (int)t)) != 0);

        switch (so.mountPolicy)
        {
            case MountPolicy.Single:
                foreach (var t in order)
                {
                    if (!socketMap.TryGetValue(t, out var list)) continue;
                    var free = list.FirstOrDefault(x => !x.occupied);
                    if (free != null) { chosen = new List<EquipSocket> { free }; return true; }
                }
                return false;

            case MountPolicy.PairSymmetric:
                if (socketMap.TryGetValue(SocketType.Left, out var Ls) &&
                    socketMap.TryGetValue(SocketType.Right, out var Rs))
                {
                    var L = Ls.FirstOrDefault(x => !x.occupied);
                    var R = Rs.FirstOrDefault(x => !x.occupied);
                    if (L != null && R != null) { chosen = new List<EquipSocket> { L, R }; return true; }
                }
                return false;

            case MountPolicy.NonOccupying:
                EquipSocket anchor = null;
                if (socketMap.TryGetValue(SocketType.Dropper, out var ds))
                    anchor = ds.FirstOrDefault(); // 점유 X
                if (anchor == null && socketMap.TryGetValue(SocketType.VehicleRoot, out var roots))
                    anchor = roots.FirstOrDefault();
                chosen = new List<EquipSocket> { anchor };
                return true;
        }
        return false;
    }

    // ========== 게이지 ==========
    private void RaiseGaugeChanged() => OnPartsGaugeChanged?.Invoke(parts, partsMax);

    private void ResetPartsGauge()
    {
        levelUpPending = false;
        parts = 0f;
        RaiseGaugeChanged();
    }

    // ========== 플레이어 업그레이드 적용 ==========
    private void ApplyPlayerUpgrade(PlayerUpgradeOption opt)
    {
        switch (opt.EffectType)
        {
            case PlayerUpgradeType.Durability: durabilityMul *= opt.Value; break; // 7101
            case PlayerUpgradeType.MaxSpeed: maxSpeedMul *= opt.Value; break; // 7102
            case PlayerUpgradeType.Acceleration: accelerationMul *= opt.Value; break; // 7103
        }

        // 플레이어에 즉시 반영 (선택 사항)
        if (player != null)
        {
            //var up = player.GetComponent<CarController>();
            //var up2 = player.GetComponent<PlayerBehaviour>();
            //if (up != null)
            //{
            //    up.ApplyMultipliers(durabilityMul, maxSpeedMul, accelerationMul);
            //}
            //if (up2 != null)
            //{
            //    up2.ApplyMultipliers(durabilityMul, maxSpeedMul, accelerationMul);
            //}

            foreach (var upg in player.GetComponents<IPlayerUpgradable>())
                upg.ApplyMultipliers(durabilityMul, maxSpeedMul, accelerationMul);
        }

        OnPlayerUpgrade?.Invoke(opt);
        Debug.Log($"[EquipManager] PlayerUpgrade: {opt.Name} x{opt.Value} -> (Dur {durabilityMul:F2}, Max {maxSpeedMul:F2}, Acc {accelerationMul:F2})");
    }

    // ========== 업그레이드 후보 생성 ==========
    public List<UpgradeCandidate> GetUpgradeCandidates(int count, WeaponLibrary lib)
    {
        var result = new List<UpgradeCandidate>(count);
        if (lib == null || lib.weapons == null || lib.weapons.Count == 0) return result;

        // 1) 무기 후보들 구성
        bool slotsFull = equips.Count >= maxEquipCount;

        var weaponPool = new List<UpgradeCandidate>();
        foreach (var w in lib.weapons)
        {
            if (w == null) continue;

            int next = GetNextLevel(w);
            if (next <= 0 || next > 5) continue; // 이미 Max면 제외

            bool alreadyEquipped = equips.Any(e => e.so == w);

            // 슬롯 가득찬 경우: 이미 장착된 무기만(=레벨업만)
            if (slotsFull && !alreadyEquipped) continue;

            // 썸네일 가져오기 (레벨별)
            Sprite thumb = null;
            var lvData = w.Levels?.FirstOrDefault(l => l.Level == next);
            if (lvData != null) thumb = lvData.ThumbNail;

            weaponPool.Add(new UpgradeCandidate
            {
                Kind = CandidateKind.Weapon,
                Weapon = w,
                NextLevel = next,
                Thumbnail = thumb
            });
        }

        // 2) 플레이어 업그레이드 3종(7101~7103)
        var playerPool = new List<UpgradeCandidate>();
        var p1 = new PlayerUpgradeOption { Id = 7101, Name = "durability_up", EffectType = PlayerUpgradeType.Durability, Value = 1.1f, Icon = iconDurability };
        var p2 = new PlayerUpgradeOption { Id = 7102, Name = "speed_up", EffectType = PlayerUpgradeType.MaxSpeed, Value = 1.1f, Icon = iconMaxSpeed };
        var p3 = new PlayerUpgradeOption { Id = 7103, Name = "acceleration_up", EffectType = PlayerUpgradeType.Acceleration, Value = 1.1f, Icon = iconAcceleration };

        playerPool.Add(ToCandidate(p1));
        playerPool.Add(ToCandidate(p2));
        playerPool.Add(ToCandidate(p3));

        // 3) 통합 풀에서 "중복 없이" 뽑기
        //    - 같은 무기 SO 중복 X, 같은 PlayerUpgrade Id 중복 X
        var pool = new List<UpgradeCandidate>();
        pool.AddRange(weaponPool);
        pool.AddRange(playerPool);

        var pickedSO = new HashSet<WeaponSO>();
        var pickedPid = new HashSet<int>();

        int tries = 0;
        int maxTries = Mathf.Max(50, pool.Count * 3);

        while (result.Count < count && tries++ < maxTries)
        {
            if (pool.Count == 0) break;
            var pick = pool[Random.Range(0, pool.Count)];

            if (pick.Kind == CandidateKind.Weapon)
            {
                if (pick.Weapon == null || pickedSO.Contains(pick.Weapon)) continue;
                pickedSO.Add(pick.Weapon);
                result.Add(pick);
            }
            else
            {
                int id = pick.PlayerUpgrade.Id;
                if (pickedPid.Contains(id)) continue;
                pickedPid.Add(id);
                result.Add(pick);
            }
        }

        // 만약 풀에 무기 후보가 거의 없고(예: 전부 Max거나 슬롯 Full+전부 미장착), 남는 자리는 플레이어 업그레이드로 채움
        int guard = 0;
        while (result.Count < count && guard++ < 10)
        {
            foreach (var pc in playerPool)
            {
                if (result.Count >= count) break;
                if (!pickedPid.Contains(pc.PlayerUpgrade.Id))
                {
                    pickedPid.Add(pc.PlayerUpgrade.Id);
                    result.Add(pc);
                }
            }
        }

        return result;
    }

    private UpgradeCandidate ToCandidate(PlayerUpgradeOption opt)
    {
        return new UpgradeCandidate
        {
            Kind = CandidateKind.PlayerStat,
            PlayerUpgrade = opt,
            Thumbnail = opt.Icon
        };
    }
    private int GetNextLevel(WeaponSO so)
    {
        var e = equips.FirstOrDefault(s => s.so == so);
        if (e == null) return 1;
        if (e.level >= 5) return -1; // Max
        return e.level + 1;
    }
    private void ChangePartsGuage()
    {
        Debug.Log("ChangePartsGuage");
        partsMax += 10;
        partsGaugeUi.SetSliderUi(parts, partsMax);
    }
}