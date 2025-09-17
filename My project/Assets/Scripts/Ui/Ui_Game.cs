using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework;
public class Ui_Game : MonoBehaviour
{
    private GameObject player;
    private WaveManager waveManager;
    private EquipManager equipManager;

    public TextMeshProUGUI waveCount;
    public TextMeshProUGUI wavePlayTime;

    public TextMeshProUGUI[] slotText;
    public Image[] slotImage;

    public List<Sprite> loadWeaponThumNails;
   

    private string[] weaponThumnailName = new string[3]; // 3: equipCount;
    private int[] weaponSOLevel = new int[3]; //
    public void Awake()
    {
        player = GameObject.FindWithTag("Player");
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindGameObjectWithTag("WaveManager").GetComponent<WaveManager>();
    }

    public void Start()
    {
        
    }

    public void Update()
    {
        wavePlayTime.text = $"{waveManager.WaveTimer:F2}";
        waveCount.text = $"{waveManager.currentWave}";

        FindWeaponSO();
        for (int i = 0; i < equipManager.Slot.Count; i++)
        {
            slotText[i].text = "Lv." + weaponSOLevel[i];
            slotImage[i].sprite = loadWeaponThumNails[i];
        }
    }

    private void FindWeaponSO()
    {
        if (equipManager.Slot.Count == 0)
        {
            return;
        }
        for (int i = 0; i < equipManager.Slot.Count; i++)
        {
            var w = equipManager.Slot[i].GetComponent<Weapon>();
            weaponThumnailName[i] = w.weaponSO.PrefabName;
            weaponSOLevel[i] = w.weaponSO.Level;
        }
    }
}