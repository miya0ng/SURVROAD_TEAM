using System;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
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

    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    void Start()
    {
        equipManager.OnEquipChanged += SetSlotInfo;
        equipManager.OnPartsGaugeChanged += UpdatePartsGauge;
        gameManager.OnSpecialPartChanged += UpdateSpecialPartUI;

        UpdatePartsGauge(equipManager.Parts, equipManager.PartsMax);
        SetSlotInfo();
    }

    void OnDestroy()
    {
        if (equipManager != null)
        {
            equipManager.OnEquipChanged -= SetSlotInfo;
            equipManager.OnPartsGaugeChanged -= UpdatePartsGauge;
        }
        if (gameManager != null)
        {
            gameManager.OnSpecialPartChanged -= UpdateSpecialPartUI;
        }
    }

    void Update()
    {
        if (player == null || waveManager == null) return;
        waveCount.text = $"{waveManager.currentWave}";
    }


    private void UpdatePartsGauge(float cur, float max)
    {
        partsGuage.SetSliderUi(cur, max);
    }

    private void UpdateSpecialPartUI(int count)
    {
        specialPartText.text = $"x {count}";
    }

    private void SetSlotInfo()
    {
        // EquipManager.Slot: IReadOnlyList<WeaponDriver>
        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < equipManager.Slot.Count && equipManager.Slot[i] != null)
            {
                var drv = equipManager.Slot[i];

                var levelData = drv.CurLevelData;
                if (levelData != null && levelData.ThumbNail != null)
                {
                    slotImage[i].sprite = levelData.ThumbNail;
                    slotImage[i].gameObject.SetActive(true);

                    slotText[i].text = $"Lv.{drv.CurLevel}";
                }
                else
                {
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                }
            }
            else
            {
                slotImage[i].sprite = null;
                slotText[i].text = string.Empty;
            }
        }
    }
}