using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Ui_Game : MonoBehaviour
{
    public WeaponLibrary weaponLibrary;

    private GameObject player;
    private WaveManager waveManager;
    private EquipManager equipManager;
    private GameManager gameManager;

    public TextMeshProUGUI waveCount;
    public TextMeshProUGUI specialPartText;

    public TextMeshProUGUI[] slotText;
    public Image[] slotImage;

    public Ui_Slider partsGuage;

    const string LOG = "[Ui_Game]";

    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null) equipManager = player.GetComponentInChildren<EquipManager>();

        var wmObj = GameObject.FindGameObjectWithTag("WaveManager");
        if (wmObj) waveManager = wmObj.GetComponent<WaveManager>();

        var gmObj = GameObject.FindGameObjectWithTag("GameManager");
        if (gmObj) gameManager = gmObj.GetComponent<GameManager>();
    }

    void OnEnable()
    {
        if (equipManager != null)
        {
            equipManager.OnEquipChanged += SetSlotInfo;
            equipManager.OnWeaponLeveled += OnWeaponLeveledRefresh;
            equipManager.OnPartsGaugeChanged += UpdatePartsGauge;
            Debug.Log($"{LOG} Subscribed to EquipManager events");

            UpdatePartsGauge(equipManager.Parts, equipManager.PartsMax);
            SetSlotInfo();
        }
        else
        {
            Debug.LogWarning($"{LOG} equipManager not found at OnEnable");
        }

        if (gameManager != null)
        {
            gameManager.OnSpecialPartChanged += UpdateSpecialPartUI;
        }
    }

    void OnDisable()
    {
        if (equipManager != null)
        {
            equipManager.OnEquipChanged -= SetSlotInfo;
            equipManager.OnWeaponLeveled -= OnWeaponLeveledRefresh;
            equipManager.OnPartsGaugeChanged -= UpdatePartsGauge;
            Debug.Log($"{LOG} Unsubscribed from EquipManager events");
        }
        if (gameManager != null)
        {
            gameManager.OnSpecialPartChanged -= UpdateSpecialPartUI;
            Debug.Log($"{LOG} Unsubscribed from GameManager events");
        }
    }

    void Update()
    {
        if (waveCount != null && waveManager != null)
            waveCount.text = $"{waveManager.currentWave}";
    }

    // === Event Handlers ===
    private void OnWeaponLeveledRefresh(WeaponDriver drv)
    {
        Debug.Log($"{LOG} OnWeaponLeveled -> {drv.weaponSO?.Name} Lv{drv.CurLevel}");
        SetSlotInfo();
    }

    private void UpdatePartsGauge(float cur, float max)
    {
        if (partsGuage == null)
        {
            Debug.LogWarning($"{LOG} partsGuage is null");
            return;
        }
        partsGuage.SetSliderUi(cur, max);
    }

    private void UpdateSpecialPartUI(int count)
    {
        if (specialPartText == null) return;
        specialPartText.text = $"x {count}";
    }

    private void SetSlotInfo()
    {
        if (equipManager == null)
        {
            Debug.LogWarning($"{LOG} SetSlotInfo aborted: equipManager is null");
            return;
        }

        var infos = equipManager.GetSlotInfos();
        int n = Mathf.Min(slotImage.Length, infos.Length);

        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < n && !infos[i].IsEmpty)
            {
                slotImage[i].sprite = infos[i].Thumbnail;
                slotImage[i].gameObject.SetActive(true);
                if (i < slotText.Length) slotText[i].text = $"Lv.{infos[i].Level}";
            }
            else
            {
                slotImage[i].sprite = null;
                slotImage[i].gameObject.SetActive(false);
                if (i < slotText.Length) slotText[i].text = string.Empty;
            }
        }
        Debug.Log($"{LOG} SetSlotInfo() infos={infos.Length}, slots={slotImage.Length}");
    }
}
