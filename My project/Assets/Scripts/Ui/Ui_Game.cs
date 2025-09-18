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

    private int curEquipCount = 0;
    private string[] weaponThumnailName = new string[3]; // 3: equipCount;

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

    //private void SortWeaponSOtoThumNail()
    //{
    //    var thumbnailDict = loadWeaponThumNails
    //        .Where(s => s != null)
    //        .GroupBy(s => s.name)
    //        .ToDictionary(g => g.Key, g => g.First());
    //    loadWeaponThumNails = weaponLibrary.weapons
    //        .Select(entry =>
    //        {
    //            string prefabName = entry.prefab ? entry.prefab.name : "";
    //            if (thumbnailDict.TryGetValue(prefabName, out var sprite))
    //                return sprite;

    //            Debug.LogWarning($"[{entry.Index}] {prefabName} ½æ³×ÀÏ ¾øÀ½");
    //            return null;
    //        })
    //        .ToList();
    //}
    private void SetSlotInfo()
    {
        Debug.Log($"[SetSlotInfo] ½½·Ô °³¼ö: {equipManager.Slot.Count}");

        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < equipManager.Slot.Count)
            {
                var w = equipManager.Slot[i].GetComponent<Weapon>();
                var so = weaponLibrary.GetSO(w.weaponSO.PrefabIndex);

                // ÇöÀç ·¹º§ µ¥ÀÌÅÍ ²¨³»¿À±â
                var levelData = so.Levels.Find(l => l.Level == w.curLevel);

                slotImage[i].sprite = so.ThumbNail;
                slotText[i].text = $"Lv.{w.curLevel}";

            }
            else
            {
                slotImage[i].sprite = null;
                slotText[i].text = string.Empty;
                Debug.Log($"[{i}] ½½·Ô ºñ¿öÁü");
            }
        }
    }
}