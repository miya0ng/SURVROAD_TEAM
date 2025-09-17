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

    public List<Sprite> loadWeaponThumNails;
   

    private string[] weaponThumnailName = new string[3]; // 3: equipCount;

    public void Awake()
    {
        player = GameObject.FindWithTag("Player");
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
    }

    public void Start()
    {
        SortWeaponSOtoThumNail();
    }

    public void Update()
    {
        if (player == null ||  waveManager == null || waveManager == null)
        {
            return;
        }
        wavePlayTime.text = $"{waveManager.WaveTimer:F2}";
        waveCount.text = $"{waveManager.currentWave}";

        SetSlotInfo();
    }

    private void SortWeaponSOtoThumNail()
    {
        var thumbnailDict = loadWeaponThumNails
            .Where(s => s != null)
            .GroupBy(s => s.name)
            .ToDictionary(g => g.Key, g => g.First());
        loadWeaponThumNails = weaponLibrary.weapons
            .Select(entry =>
            {
                string prefabName = entry.prefab ? entry.prefab.name : "";
                if (thumbnailDict.TryGetValue(prefabName, out var sprite))
                    return sprite;

                Debug.LogWarning($"[{entry.Index}] {prefabName} ½æ³×ÀÏ ¾øÀ½");
                return null;
            })
            .ToList();
    }
    private void SetSlotInfo()
    {
        if (equipManager.Slot.Count == 0)
        {
            return;
        }

        for (int i = 0; i < equipManager.Slot.Count; i++)
        {
            var w = equipManager.Slot[i].GetComponent<Weapon>();
            slotText[i].text = "Lv." + w.weaponSO.Level;
            Debug.Log((int)w.weaponSO.PrefabIndex);
            slotImage[i].sprite = loadWeaponThumNails[(int)w.weaponSO.PrefabIndex];
        }
    }
}