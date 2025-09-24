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

        var weaponManager = GameObject.FindWithTag("WaveManager");
        if (weaponManager) waveManager = weaponManager.GetComponent<WaveManager>();

        var enemySpawner = GameObject.FindWithTag("EnemySpawner");
        if (enemySpawner) this.enemySpawner = enemySpawner.GetComponent<EnemySpawner>();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!player || !playerController || !playerHp)
            return;

        if (weaponName) weaponName.text = "WeaponName: ";
        if (speed) speed.text = "Speed: " + playerController.velLocal.ToString("F2");
        if (hp) hp.text = $"Hp: {playerHp.curHp}/{playerHp.maxHp}";

        if (waveManager && waveCount)
        {
            waveCount.text = "WaveCount: " + waveManager.currentWave;
            if (timePerWave) timePerWave.text = $"TimePerWave: {waveManager.WaveTimer:F2}";
        }

        if (enemySpawner && leftEnemy)
        {
            int remainingToSpawn = Mathf.Max(0, enemySpawner.waveSpawnCount - enemySpawner.curSpawnCount);
            leftEnemy.text =
                $"SpawnLeft: {remainingToSpawn}, Active: {enemySpawner.ActiveEnemyCount}, " +
                $"Spawned: {enemySpawner.curSpawnCount}/{enemySpawner.waveSpawnCount}";
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
#else
        if (gameObject.activeSelf) gameObject.SetActive(false);
#endif
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
