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

    public TextMeshProUGUI waveCount;
    public TextMeshProUGUI wavePlayTime;

    public TextMeshProUGUI[] slotText;
    public Image[] slotImage;

    public void Awake()
    {
        player = GameObject.FindWithTag("Player");
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
    }

    public void Start()
    {
        //SortWeaponSOtoThumNail();
        equipManager.OnEquipChanged += SetSlotInfo;
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

    private void SetSlotInfo()
    {
#if DEBUG_MODE
        Debug.Log($"[SetSlotInfo] ���� ����: {equipManager.Slot.Count}");

        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < equipManager.Slot.Count && equipManager.Slot[i] != null)
            {
                var go = equipManager.Slot[i];
                var w = go.GetComponent<Weapon>();

                if (w == null)
                {
                    Debug.LogWarning($"[Slot {i}] GameObject={go.name}, Weapon ������Ʈ ����");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                if (w.weaponSO == null)
                {
                    Debug.LogWarning($"[Slot {i}] {go.name}: weaponSO ����");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                if (w.CurLevelData == null)
                {
                    Debug.LogWarning($"[Slot {i}] {w.weaponSO.Name} CurLevelData ���� (curLevel?)");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                if (w.CurLevelData.ThumbNail == null)
                {
                    Debug.LogWarning($"[Slot {i}] {w.weaponSO.Name} Lv{w.CurLevelData.Level} ����� ����");
                    slotImage[i].sprite = null;
                    slotText[i].text = string.Empty;
                    continue;
                }

                // ���� ó��
                Debug.Log($"[Slot {i}] {w.weaponSO.Name} Lv{w.CurLevelData.Level}, ����� OK");
                slotImage[i].sprite = w.CurLevelData.ThumbNail;
                slotText[i].text = $"Lv.{w.CurLevelData.Level}";
            }
            //else
            //{
            //    Debug.Log($"[Slot {i}] ���� �������");
            //    slotImage[i].sprite = null;
            //    slotText[i].text = string.Empty;
            //}
        }
#else
        Debug.Log($"[SetSlotInfo] ���� ����: {equipManager.Slot.Count}");
        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < equipManager.Slot.Count && equipManager.Slot[i] != null)
            {
                var w = equipManager.Slot[i].GetComponent<Weapon>();
                if (w != null && w.weaponSO != null)
                {
                    slotImage[i].sprite = w.weaponSO.Levels[w.CurLevelData.Level - 1].ThumbNail;
                    slotText[i].text = $"Lv.{w.CurLevelData.Level}";
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
      
#endif
    }
}
