#define DEBUG_MODE
using NUnit.Framework;
using System.Collections.Generic;
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
    public TextMeshProUGUI wavePlayTime;
    public TextMeshProUGUI specialPartText;

    public TextMeshProUGUI[] slotText;
    public Image[] slotImage;

    public void Awake()
    {
        player = GameObject.FindWithTag("Player");
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
       
    }

    public void Start()
    {
        //SortWeaponSOtoThumNail();
        equipManager.OnEquipChanged += SetSlotInfo;
        gameManager.OnSpecialPartChanged += UpdateSpecialPartUI;
    }

    public void Update()
    {
        if (player == null || waveManager == null || waveManager == null)
        {
            return;
        }
        wavePlayTime.text = $"{waveManager.WaveTimer:F2}";
        waveCount.text = $"{waveManager.currentWave}";
    }
    private void UpdateSpecialPartUI(int count)
    {
        specialPartText.text = $"x {count}";
    }

    private void SetSlotInfo()
    {
        Debug.Log($"[SetSlotInfo] 슬롯 개수: {equipManager.Slot.Count}");

        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < equipManager.Slot.Count && equipManager.Slot[i] != null)
            {
                var go = equipManager.Slot[i];
                var w = go.GetComponent<Weapon>();

                if (w == null)
                {
                    Debug.LogWarning($"[Slot {i}] GameObject={go.name}, Weapon 컴포넌트 없음");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                if (w.weaponSO == null)
                {
                    Debug.LogWarning($"[Slot {i}] {go.name}: weaponSO 없음");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                if (w.CurLevelData == null)
                {
                    Debug.LogWarning($"[Slot {i}] {w.weaponSO.Name} CurLevelData 없음 (curLevel?)");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                if (w.CurLevelData.ThumbNail == null)
                {
                    Debug.LogWarning($"[Slot {i}] {w.weaponSO.Name} Lv{w.CurLevelData.Level} 썸네일 없음");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                // 정상 처리
                Debug.Log($"[Slot {i}] {w.weaponSO.Name} Lv{w.CurLevelData.Level}, 썸네일 OK");
                slotImage[i].sprite = w.CurLevelData.ThumbNail;
                slotText[i].text = $"Lv.{w.CurLevelData.Level}";
            }

        }
    }
}
