using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LevelUpPopup : MonoBehaviour
{
    [SerializeField] private EquipManager equip;
    [SerializeField] private WeaponLibrary weaponLibrary;

    private int choiceCount = 3; // 선택창 수

    public Button equipButton;

    public Button choice0;
    public Button choice1;
    public Button choice2;

    private int selectedIndex = -1;
    public Image[] weaponIcons;
    private (WeaponSO so, int level)[] candidates;

    int[] weaponIds;

    void OnEnable()
    {
        equip.OnLevelUpReady += Show;
        equip.OnPartsGaugeChanged += UpdateGauge;
    }
    void OnDisable()
    {
        Time.timeScale = 1;
        equip.OnLevelUpReady -= Show;
        equip.OnPartsGaugeChanged -= UpdateGauge;
        selectedIndex = -1;
    }

    void UpdateGauge(float cur, float max)
    {
        // 게이지 UI 반영
        // 업그레이드 창이 나타날 때마다 max gauage가 달라짐
    }

    void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0;
        weaponIds = new int[choiceCount];
        candidates = new (WeaponSO, int)[choiceCount];

        // 후보 만들기 (현재 장착여부 따라 Lv1 or CurLevel+1)
        var allCandidates = new List<(WeaponSO so, int level)>();
        foreach (var w in weaponLibrary.weapons)
        {
            var equipped = equip.Slot.FirstOrDefault(s => s.weaponSO == w);
            int nextLevel = equipped != null ? Mathf.Min(equipped.CurLevel + 1, 5) : 1;
            allCandidates.Add((w, nextLevel));
        }

        // 랜덤으로 3개 뽑기
        for (int i = 0; i < choiceCount; i++)
        {
            var pick = allCandidates[Random.Range(0, allCandidates.Count)];
            candidates[i] = pick;
            weaponIds[i] = pick.so.ID;
            weaponIcons[i].sprite = weaponLibrary.GetThumbnail(pick.so.Levels[pick.level-1].PrefabIndex, pick.level);
        }
    }

    public void OnClick_slot0()
    {
        selectedIndex = 0;
    }
    public void OnClick_slot1()
    {
        selectedIndex = 1;
    }
    public void OnClick_slot2()
    {
        selectedIndex = 2;
    }

    public void OnClick_EquipButton()
    {
        if (selectedIndex < 0)
        {
            Debug.Log("선택된 무기가 없음");
            return;
        }

        var (so, level) = candidates[selectedIndex];
        var equipped = equip.Slot.FirstOrDefault(s => s.weaponSO == so);

        if (equipped != null)
        {
            int idx = -1;
            if (equip.Slot is List<WeaponDriver> list)
            {
               idx = list.IndexOf(equipped);
            }
            equip.ApplyLevelUpChoice_LevelUpExisting(idx);
        }
        else
        {
            var data = so.Levels.FirstOrDefault(l => l.Level == level);
            if (data != null && data.prefab != null)
                equip.ApplyLevelUpChoice_EquipNew(data.prefab, so);
        }

        gameObject.SetActive(false);
    }
}