using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static EquipManager;

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

    private int selectedIndex = -1;
    private UpgradeCandidate[] candidates;
    private bool isOpen = false;

    void OnEnable()
    {
        if (equip == null)
        {
            return;
        }
        equip.OnLevelUpReady += Show;
        equip.OnPartsGaugeChanged += UpdateGauge;
        if (equipButton != null) equipButton.interactable = false;
    }

    void OnDisable()
    {
        if (isOpen) CloseInternal();

        if (equip == null)
        {
            return;
        }
        equip.OnLevelUpReady -= Show;
        equip.OnPartsGaugeChanged -= UpdateGauge;
    }

    // ===== Events =====
    void UpdateGauge(float cur, float max)
    {
        // 게이지 UI 반영 (필요시)
    }

    // ===== Show/Hide =====
    void Show()
    {
        if (equip == null || weaponLibrary == null || weaponIcons == null)
        {
            return;
        }

        var list = equip.GetUpgradeCandidates(choiceCount, weaponLibrary);
        candidates = list?.ToArray() ?? System.Array.Empty<UpgradeCandidate>();

        int n = Mathf.Min(candidates.Length, weaponIcons.Length);
        for (int i = 0; i < weaponIcons.Length; i++)
        {
            if (i < n && candidates[i].Thumbnail != null)
            {
                weaponIcons[i].sprite = candidates[i].Thumbnail;
                weaponIcons[i].gameObject.SetActive(true);
            }
            else
            {
                weaponIcons[i].sprite = null;
                weaponIcons[i].gameObject.SetActive(false);
            }
        }

        selectedIndex = -1;
        if (equipButton != null)
        {
            equipButton.interactable = false;
        }

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
        if (candidates == null || idx < 0 || idx >= candidates.Length)
        {
            return;
        }

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
        equip.EquipOrUpgrade(c);

        CloseInternal();
    }
}