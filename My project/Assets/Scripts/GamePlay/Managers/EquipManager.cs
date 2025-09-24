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
    [SerializeField] private int maxEquipCount = 3;

    private LivingEntity player;

    // 슬롯 = 논리 장착(가상/물리 포함), driver가 null이면 가상 장착
    private class EquippedEntry
    {
        public WeaponSO so;
        public int level;
        public WeaponDriver driver;// 대표 드라이버(하위 호환용)
        public List<WeaponDriver> drivers = new(); // ★ 모든 인스턴스(좌/우 포함)
        public List<EquipSocket> sockets;// 점유 소켓(NonOccupying은 null/빈 리스트)
        public bool IsMounted => driver != null;
    }

    private readonly List<EquippedEntry> equips = new(); // UI는 이 리스트를 슬롯으로 봄
    public int EquippedCount => equips.Count;

    // (하위 호환) 기존처럼 드라이버 배열을 노출 — 가상 슬롯은 null로 표시
    public IReadOnlyList<WeaponDriver> Slot => equips.Select(e => e.driver).ToList();

    // 소켓 인덱스화
    private readonly Dictionary<SocketType, List<EquipSocket>> socketMap = new();

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
        public bool IsEmpty => Thumbnail == null && string.IsNullOrEmpty(Name);
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

        // 플레이어 하위에서 EquipSocket 수집 (비활성 포함)
        socketMap.Clear();
        var sockets = player ? player.GetComponentsInChildren<EquipSocket>(true)
                             : GetComponentsInChildren<EquipSocket>(true);
        foreach (var s in sockets)
        {
            if (!socketMap.TryGetValue(s.type, out var list)) socketMap[s.type] = list = new();
            list.Add(s);
        }

        // 스타터 로드아웃 (SO 기반 장착 권장 / 소켓 없으면 "가상 장착")
        if (equipStarterOnStart && starterSO != null && player != null)
        {
            EquipWeapon(starterSO, 1);
        }
        else if (equipStarterOnStart && starterPrefab != null && starterSO != null && player != null)
        {
            // 하위 호환: 직접 프리팹 경로 — 소켓 배정 시도 후 없으면 가상장착
            EquipWeapon(starterPrefab, starterSO, player);
        }

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

    // 팝업 선택(기존 무기 레벨업)
    public void ApplyNewEquipExisting(int slotIndex)
    {
        if (!levelUpPending || !IsValidSlotIndex(slotIndex)) return;

        var entry = equips[slotIndex];
        entry.level = Mathf.Min(entry.level + 1, 5);

        // 물리 장착된 모든 드라이버에 레벨 반영
        if (entry.drivers != null && entry.drivers.Count > 0)
        {
            foreach (var d in entry.drivers)
                if (d && d.SetLevel(entry.level)) OnWeaponLeveled?.Invoke(d);
        }

        // 대표 드라이버도 동기화(하위 호환)
        entry.driver = entry.drivers.FirstOrDefault();
        OnEquipChanged?.Invoke();
        ResetPartsGauge();
    }

    // 팝업 선택(신규 장착)
    public void ApplyNewEquip(GameObject weaponPrefab, WeaponSO so)
    {
        if (!levelUpPending || weaponPrefab == null || so == null) return;
        EquipNewCore(so, 1, preferPhysical: true); // 소켓 없으면 가상으로 들어감
        ResetPartsGauge();
    }

    // 통합 선택 API
    public void EquipOrUpgrade(UpgradeCandidate c)
    {
        if (!levelUpPending || c.Weapon == null) return;

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

    // 외부에서 직접 장착 호출(동일 SO면 자동 레벨업)
    public void EquipWeapon(GameObject weaponPrefab, WeaponSO so, LivingEntity owner)
    {
        int sameIdx = FindIndexBySO(so);
        if (sameIdx >= 0) { ApplyNewEquipExisting(sameIdx); return; }

        // 소켓 시도 → 실패 시 가상 장착
        if (!TryGetLevelData(so, 1, out var data) || data.prefab == null) return;
        TryMountOrVirtual(so, 1, preferPhysical: true);
    }

    // 추천 경로: SO + 레벨 (소켓 없으면 자동 가상)
    public void EquipWeapon(WeaponSO so, int level = 1)
    {
        int sameIdx = FindIndexBySO(so);
        if (sameIdx >= 0)
        {
            equips[sameIdx].level = Mathf.Min(equips[sameIdx].level + 1, 5);
            SyncMountedLevel(sameIdx);
            OnEquipChanged?.Invoke();
            return;
        }
        TryMountOrVirtual(so, level, preferPhysical: true);
    }

    // 슬롯 정보(UI 표시용) — 드라이버 없을 때도 SO/레벨로 썸네일/이름 제공
    public WeaponSlotInfo[] GetSlotInfos()
    {
        var list = new List<WeaponSlotInfo>(maxEquipCount);
        for (int i = 0; i < maxEquipCount; i++)
        {
            if (i >= equips.Count || equips[i] == null)
            {
                list.Add(new WeaponSlotInfo()); // 빈 슬롯
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

    // 교체: 물리 장착이면 모두 파기 후 재장착, 가상이면 데이터만 변경
    public void ReplaceWeapon(int index, GameObject newWeaponPrefab, WeaponSO so)
    {
        if (!IsValidSlotIndex(index) || newWeaponPrefab == null || so == null) return;

        var entry = equips[index];

        // 기존 물리 장착 모두 파괴
        if (entry.drivers != null)
        {
            foreach (var d in entry.drivers)
            {
                if (d) OnWeaponUnequipped?.Invoke(d);
                if (d) Destroy(d.gameObject);
            }
            entry.drivers.Clear();
        }
        entry.driver = null;
        ReleaseSockets(entry);

        // 데이터 교체
        entry.so = so;
        entry.level = Mathf.Max(1, entry.level);

        // 새 소켓 시도 → 실패 시 가상 유지
        TryMountIntoExistingEntry(entry, preferPhysical: true);
        equips[index] = entry;
        OnEquipChanged?.Invoke();
    }

    public void UnEquipWeapon(int index)
    {
        if (!IsValidSlotIndex(index)) return;

        var entry = equips[index];

        // 모든 드라이버 제거 & 이벤트 발행
        if (entry.drivers != null)
        {
            foreach (var d in entry.drivers)
            {
                if (d) OnWeaponUnequipped?.Invoke(d);
                if (d) Destroy(d.gameObject);
            }
        }

        ReleaseSockets(entry);
        equips.RemoveAt(index);
        OnEquipChanged?.Invoke();
        ResetPartsGauge();
    }

    public void UnEquipLast()
    {
        if (equips.Count == 0) return;
        UnEquipWeapon(equips.Count - 1);
    }

    // ============= cheat =================
    public void ForceEquipNew(WeaponSO so, int level = 1)
    {
        bool prevPending = levelUpPending;
        levelUpPending = false;
        EquipWeapon(so, level);
        levelUpPending = prevPending;
    }

    // 소켓이 생겼을 때(예: 어떤 무기 해제) 가상 장착을 물리화 시도
    public void TryMaterializeMounts()
    {
        foreach (var e in equips.Where(x => !x.IsMounted).ToList())
        {
            TryMountIntoExistingEntry(e, preferPhysical: true);
        }
        OnEquipChanged?.Invoke();
    }

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
            entry.driver = created.FirstOrDefault(); // 대표 드라이버(하위 호환)
            entry.sockets = socketsUsed;

            foreach (var d in created) OnWeaponEquipped?.Invoke(d);
        }
        else
        {
            // 가상 장착(소켓 없음)
            entry.driver = null;
            entry.drivers.Clear();
            entry.sockets = null;
        }

        equips.Add(entry);
        OnEquipChanged?.Invoke();
    }

    private void TryMountIntoExistingEntry(EquippedEntry entry, bool preferPhysical)
    {
        if (!preferPhysical || entry.so == null) return;

        if (TryAssignMount(entry.so, out var chosen))
        {
            var created = CreateDriversByPolicy(entry.so, entry.level, chosen, out var socketsUsed);
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
    }

    private List<WeaponDriver> CreateDriversByPolicy(WeaponSO so, int level, List<EquipSocket> chosen, out List<EquipSocket> socketsUsed)
    {
        socketsUsed = new List<EquipSocket>();

        if (!TryGetLevelData(so, level, out var data) || data.prefab == null)
            return new List<WeaponDriver>();

        switch (so.MountPolicy)
        {
            case MountPolicy.Single:
                {
                    var sock = chosen[0];
                    var parent = (sock != null) ? (sock.mount ? sock.mount : sock.transform) : transform;
                    var drv = CreateWeaponInstance(data.prefab, so, level, parent);
                    if (sock != null) { sock.occupied = true; socketsUsed.Add(sock); }
                    return new List<WeaponDriver> { drv };
                }
            case MountPolicy.PairSymmetric:
                {
                    var left = chosen[0]; var right = chosen[1];
                    var lt = left.mount ? left.mount : left.transform;
                    var rt = right.mount ? right.mount : right.transform;

                    var drvL = CreateWeaponInstance(data.prefab, so, level, lt);
                    var drvR = CreateWeaponInstance(data.prefab, so, level, rt);

                    // 좌/우 소켓 점유 표시
                    left.occupied = right.occupied = true;
                    socketsUsed.Add(left);
                    socketsUsed.Add(right);

                    // 좌우 차등 로직이 필요하면 여기서 처리(옵션)
                    // var sockTypeL = left.type; var sockTypeR = right.type;
                    // ex) 스케일 반전, 회전 방향 등

                    return new List<WeaponDriver> { drvL, drvR }; // ★ 두 개 모두 반환
                }
            case MountPolicy.NonOccupying:
                {
                    var anchor = chosen[0]; // 점유 X
                    var parent = (anchor != null) ? (anchor.mount ? anchor.mount : anchor.transform) : transform;
                    var drv = CreateWeaponInstance(data.prefab, so, level, parent);
                    // occupied 안 건드림
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
                if (d && d.SetLevel(e.level)) OnWeaponLeveled?.Invoke(d);
        }
        e.driver = e.drivers.FirstOrDefault();
    }

    private bool IsValidSlotIndex(int index) => index >= 0 && index < equips.Count;

    private int FindIndexBySO(WeaponSO so)
        => so == null ? -1 : equips.FindIndex(w => w != null && w.so == so);

    private WeaponDriver CreateWeaponInstance(GameObject prefab, WeaponSO so, int level, Transform parent)
    {
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

        switch (so.MountPolicy)
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
                    anchor = ds.FirstOrDefault(); // 점유X
                if (anchor == null && socketMap.TryGetValue(SocketType.VehicleRoot, out var roots))
                    anchor = roots.FirstOrDefault();
                chosen = new List<EquipSocket> { anchor }; // null일 수도 있지만 드라이버가 부모 transform 사용
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

    // ========== 업그레이드 후보 ==========
    public List<UpgradeCandidate> GetUpgradeCandidates(int count, WeaponLibrary lib)
    {
        if (lib == null || lib.weapons == null) return new List<UpgradeCandidate>();

        var all = new List<UpgradeCandidate>(lib.weapons.Count);
        foreach (var w in lib.weapons)
            all.Add(BuildCandidate(w, lib));

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

    private UpgradeCandidate BuildCandidate(WeaponSO so, WeaponLibrary lib)
    {
        if (so == null || lib == null) return default;

        int next = GetNextLevel(so);
        var levelData = (so.Levels != null && so.Levels.Count >= next) ? so.Levels[next - 1] : null;
        var thumb = (levelData != null) ? lib.GetThumbnail(levelData.PrefabIndex, next) : null;

        return new UpgradeCandidate { Weapon = so, NextLevel = next, Thumbnail = thumb };
    }

    private int GetNextLevel(WeaponSO so)
    {
        var e = equips.FirstOrDefault(s => s.so == so);
        return e != null ? Mathf.Min(e.level + 1, 5) : 1;
    }
}
