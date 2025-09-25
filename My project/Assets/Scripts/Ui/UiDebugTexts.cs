using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiDebugTexts : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI hp;
    public TextMeshProUGUI speed;
    public TextMeshProUGUI weaponName;

    public TextMeshProUGUI waveCount;
    public TextMeshProUGUI timePerWave;
    public TextMeshProUGUI leftEnemy;
    public TextMeshProUGUI weaponSOText;

    private GameObject player;
    private CarController playerController;
    private PlayerBehaviour playerHp;
    private WaveManager waveManager;
    private EnemySpawner enemySpawner;
    private EquipManager equipManager;

    private string[] equipWeapons = new string[3];

    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<CarController>();
            playerHp = player.GetComponent<PlayerBehaviour>();
            equipManager = player.GetComponentInChildren<EquipManager>();
        }

        var wmObj = GameObject.FindWithTag("WaveManager");
        if (wmObj) waveManager = wmObj.GetComponent<WaveManager>();

        var esObj = GameObject.FindWithTag("EnemySpawner");
        if (esObj) enemySpawner = esObj.GetComponent<EnemySpawner>();
    }

    void Update()
    {

        if (!player || !playerController || !playerHp)
            return;

        if (weaponName) weaponName.text = "WeaponName: ";
        if (speed) speed.text = "Speed: " + playerController.velLocal.ToString("F2");
        if (hp) hp.text = $"Hp: {playerHp.curHp}/{playerHp.maxHp}";

        if (waveManager && waveCount)
        {
            if (waveManager.TotalWaves > 0)
                waveCount.text = $"Wave: {waveManager.CurrentWaveNumber}/{waveManager.TotalWaves}";
      
            if (timePerWave) timePerWave.text = $"TimePerWave: {waveManager.WaveTimer:F2}";
        }

        if (enemySpawner && leftEnemy)
        {
            int total = enemySpawner.WaveTotalToSpawn;
            int spawned = enemySpawner.SpawnedInWave;
            int remainingToSpawn = Mathf.Max(0, total - spawned);

            leftEnemy.text =
                $"SpawnLeft: {remainingToSpawn}, Active: {enemySpawner.ActiveEnemyCount}, " +
                $"Spawned: {spawned}/{total}";
        }

        if (equipManager != null && equipManager.Slot != null && equipManager.Slot.Count > 0)
        {
            FillWeaponNames();
            if (weaponSOText) weaponSOText.text = "WeaponSO: " + string.Join(", ", equipWeapons);
        }
        else
        {
            if (weaponSOText) weaponSOText.text = "WeaponSO: None";
        }

    }

    private void FillWeaponNames()
    {
        for (int i = 0; i < equipWeapons.Length; i++) equipWeapons[i] = "-";

        int count = Mathf.Min(equipManager.Slot.Count, equipWeapons.Length);
        for (int i = 0; i < count; i++)
        {
            var drv = equipManager.Slot[i];
            if (drv == null || drv.weaponSO == null || drv.CurLevelData == null)
            {
                equipWeapons[i] = "null";
                continue;
            }
            equipWeapons[i] = $"{drv.weaponSO.Name}(Lv{drv.CurLevel})";
        }
    }
}
