using UnityEngine;

public class LevelUpPopup : MonoBehaviour
{
    [SerializeField] private EquipManager equip;

    void OnEnable()
    {
        equip.OnLevelUpReady += Show;
        equip.OnPartsGaugeChanged += UpdateGauge;
    }
    void OnDisable()
    {
        equip.OnLevelUpReady -= Show;
        equip.OnPartsGaugeChanged -= UpdateGauge;
    }

    void UpdateGauge(float cur, float max)
    {
        // 게이지 UI 반영
    }

    void Show()
    {
        // 팝업 열기 + 옵션 구성
        // ① 기존 무기 중 하나 레벨업
        // ② 새로운 무기 1개 선택하여 장착
    }

    public void OnClick_LevelUpSlot0() => equip.ApplyLevelUpChoice_LevelUpExisting(0);
    public void OnClick_LevelUpSlot1() => equip.ApplyLevelUpChoice_LevelUpExisting(1);
    public void OnClick_EquipNew(GameObject weaponPrefab, WeaponSO so)
        => equip.ApplyLevelUpChoice_EquipNew(weaponPrefab, so);
}