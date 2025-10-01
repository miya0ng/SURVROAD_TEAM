using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static EquipManager;
using TMPro;
public class LevelUpPopup : MonoBehaviour
{
    [SerializeField] private EquipManager equip;
    [SerializeField] private WeaponLibrary weaponLibrary;

    [Header("UI")]
    [SerializeField] private int choiceCount = 3;
    [SerializeField] private Button equipButton;
    [SerializeField] private Image[] weaponIcons;
    [SerializeField] private GameObject levelUpDefault;
    [SerializeField] private GameObject DetailPopUp;

    [Header("Selection Info")]
    [SerializeField] private TextMeshProUGUI[] weaponNameLabels;
    [SerializeField] private TextMeshProUGUI[] weaponTypeLabels;
    [SerializeField] private TextMeshProUGUI[] selectionInfoLabels;

    private int selectedIndex = -1;
    private UpgradeCandidate[] candidates;
    private bool isOpen = false;

    void OnEnable()
    {
        if (equip == null) return;

        equip.OnLevelUpReady += Show;
        equip.OnPartsGaugeChanged += UpdateGauge;
        if (equipButton != null) equipButton.interactable = false;
    }

    void OnDisable()
    {
        if (isOpen) CloseInternal();

        if (equip == null) return;
        equip.OnLevelUpReady -= Show;
        equip.OnPartsGaugeChanged -= UpdateGauge;
    }
 
    // ===== Events =====
    void UpdateGauge(float cur, float max)
    {
        // 게이지 UI 반영 필요 시 작성
    }

    // ===== Show/Hide =====
    void Show()
    {
        if (equip == null || weaponLibrary == null || weaponIcons == null) return;

        var list = equip.GetUpgradeCandidates(choiceCount, weaponLibrary);
        candidates = list?.ToArray() ?? System.Array.Empty<UpgradeCandidate>();
        int n = Mathf.Min(candidates.Length, weaponIcons.Length);

        for (int i = 0; i < weaponIcons.Length; i++)
        {
            bool visible = i < n;
            var icon = weaponIcons[i];

            if (visible)
            {
                var c = candidates[i];
                c.SelectInfo = GetSelectionInfoSafe(c);
                candidates[i] = c;

                // === 아이콘/텍스트 바인딩 ===
                icon.sprite = c.Thumbnail;
                icon.gameObject.SetActive(true);

                if (weaponNameLabels != null && i < weaponNameLabels.Length && weaponNameLabels[i] != null)
                    weaponNameLabels[i].text = GetDisplayName(c);

                if (weaponTypeLabels != null && i < weaponTypeLabels.Length && weaponTypeLabels[i] != null)
                    weaponTypeLabels[i].text = GetDisplayType(c);

                if (selectionInfoLabels != null && i < selectionInfoLabels.Length && selectionInfoLabels[i] != null)
                    selectionInfoLabels[i].text = c.SelectInfo;
            }
            else
            {
                icon.sprite = null;
                icon.gameObject.SetActive(false);

                if (weaponNameLabels != null && i < weaponNameLabels.Length && weaponNameLabels[i] != null)
                    weaponNameLabels[i].text = string.Empty;
                if (weaponTypeLabels != null && i < weaponTypeLabels.Length && weaponTypeLabels[i] != null)
                    weaponTypeLabels[i].text = string.Empty;
                if (selectionInfoLabels != null && i < selectionInfoLabels.Length && selectionInfoLabels[i] != null)
                    selectionInfoLabels[i].text = string.Empty;
            }
        }

        selectedIndex = -1;
        if (equipButton != null) equipButton.interactable = false;

        if (levelUpDefault != null) levelUpDefault.SetActive(true);
        Time.timeScale = 0;
        isOpen = true;
    }

    private void CloseInternal()
    {
        Time.timeScale = 1;
        if (levelUpDefault != null) levelUpDefault.SetActive(false);
        selectedIndex = -1;
        isOpen = false;
    }

    // ===== Slot Clicks =====
    public void OnClick_slot0() => SelectIndex(0);
    public void OnClick_slot1() => SelectIndex(1);
    public void OnClick_slot2() => SelectIndex(2);

    private void SelectIndex(int idx)
    {
        if (candidates == null || idx < 0 || idx >= candidates.Length) return;

        if (selectedIndex == idx)
        {
            selectedIndex = -1;
            if (equipButton != null) equipButton.interactable = false;
            return;
        }

        selectedIndex = idx;
        if (equipButton != null) equipButton.interactable = true;
    }

    public void OnClick_EquipButton()
    {
        if (selectedIndex < 0 || candidates == null || selectedIndex >= candidates.Length)
        {
            Debug.LogWarning($"EquipButton aborted: invalid selection (idx={selectedIndex}, len={(candidates == null ? -1 : candidates.Length)})");
            return;
        }

        var c = candidates[selectedIndex];
        equip.EquipOrUpgrade(c);   // 무기/플레이어 업그레이드 모두 처리
        CloseInternal();
    }
    private static string GetDisplayName(UpgradeCandidate c)
    {
        if (c.Kind == CandidateKind.Weapon && c.Weapon != null)
        {
            // 요구사항: "이름은 Name 프로퍼티 사용"
            return c.Weapon.Name;
        }
        // Player 업그레이드: "durability_up" → "Durability Up" 등
        var raw = c.PlayerUpgrade.Name ?? "";
        return ToTitleFromSnake(raw);
    }

    private static string GetDisplayType(UpgradeCandidate c)
    {
        if (c.Kind == CandidateKind.Weapon && c.Weapon != null)
            return TypeToString(c.Weapon.Type);
        return "Player";
    }

    private static string TypeToString(int type)
    {
        switch (type)
        {
            case 1: return "Gun";
            case 2: return "Melee";
            case 3: return "Trap";
            default: return "Unknown";
        }
    }

    private static string ToTitleFromSnake(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var parts = s.Split('_');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : "");
        }
        return string.Join(" ", parts);
    }

    private string GetSelectionInfoSafe(UpgradeCandidate c)
    {
        // 플레이어 업그레이드
        if (c.Kind == CandidateKind.PlayerStat)
        {
            var raw = c.PlayerUpgrade.Name ?? "";
            var display = ToTitleFromSnake(raw);
            var pct = (c.PlayerUpgrade.Value - 1f) * 100f;
            return $"{display} + {pct:F1}%";
        }

        // 무기: SO의 해당 레벨에서 SelectionInfo 사용
        if (c.Kind != CandidateKind.Weapon || c.Weapon == null)
            return string.Empty;

        int level = Mathf.Max(1, c.NextLevel);
        var lv = c.Weapon.Levels?.FirstOrDefault(l => l.Level == level);

        return string.IsNullOrEmpty(lv?.SelectionInfo) ? string.Empty : lv.SelectionInfo;
    }
}