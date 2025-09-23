using UnityEngine;
using UnityEngine.UI;

public class LevelUpPopup : MonoBehaviour
{
    [SerializeField] private EquipManager equip;
    [SerializeField] private WeaponLibrary weaponLibrary;

    private int choiceCount = 3; // 선택창 수

    public Button equipButton;

    public Button choice0;
    public Button choice1;
    public Button choice2;

    public int idx;
    public Sprite[] weaponIcons;
    int[] weaponIds;

    private GameObject weaponPrefab;
    private WeaponSO so;
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
        for (int i = 0; i < choiceCount; i++)
        {
            idx = Random.Range(0, weaponLibrary.weapons.Count);
            weaponIds[i] = weaponLibrary.weapons[idx].ID;
            weaponIcons[i] = weaponLibrary.GetThumbnail((WeaponIndex)idx, 1);
        }
        // 팝업 열기 + 옵션 구성
        // ① 기존 무기 중 하나 레벨업
        // ② 새로운 무기 1개 선택하여 장착

        //선택한 choice인덱스 찾기
    }

    public void OnClick_LevelUpSlot0()
    {
        int iiiiiiiiiiiiiiiii  = weaponLibrary.weapons[0].ID;
    }
    public void OnClick_LevelUpSlot1()
    {
        
    }
    public void OnClick_LevelUpSlot2()
    {
       
    }
    public void OnClick_EquipButton(GameObject weaponPrefab, WeaponSO so)
    {
        equip.ApplyLevelUpChoice_EquipNew(weaponPrefab, so);
        gameObject.SetActive(false);
    }
}