using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiDebugTexts : MonoBehaviour
{
    public TextMeshProUGUI hp;
    public TextMeshProUGUI speed;
    public TextMeshProUGUI weaponName;

    public TextMeshProUGUI waveCount;
    public TextMeshProUGUI timePerWave;
    public TextMeshProUGUI leftEnemy;
    public TextMeshProUGUI weaponSOText;

    private GameObject player;
    private PlayerController playerController;
    private PlayerBehaviour playerHp;
    private WaveManager waveManager;
    private EnemySpawner enemySpawner;
    private EquipManager equipManager;

    private string[] weaponSOName = new string[3];

    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerHp = player.GetComponent<PlayerBehaviour>();
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindWithTag("WaveManager").GetComponent<WaveManager>();
        enemySpawner = GameObject.FindWithTag("EnemySpawner").GetComponent<EnemySpawner>();
    }

    void Update()
    {
#if UNITY_EDITOR
        // 기본 스탯 표시
        weaponName.text = "WeaponName: ";
        speed.text = "Speed: " + playerController.curMoveSpeed;
        hp.text = "Hp: " + playerHp.curHp + "/" + playerHp.maxHp;

        // 웨이브 정보 표시
        waveCount.text = "WaveCount: " + waveManager.currentWave;
        timePerWave.text = $"TimePerWave: {waveManager.WaveTimer:F2}";
        leftEnemy.text = "LeftEnemy: " + enemySpawner.ActiveEnemyCount + "/" + enemySpawner.waveSpawnCount;

        // 장착 무기 정보 표시
        if (equipManager.Slot.Count > 0)
        {
            FindWeaponSOName();
            weaponSOText.text = "WeaponSO: " + string.Join(", ", weaponSOName);
        }
        else
        {
            weaponSOText.text = "WeaponSO: None";
        }
#else
        if (gameObject.activeSelf) gameObject.SetActive(false);
#endif
    }

    private void FindWeaponSOName()
    {
        for (int i = 0; i < equipManager.Slot.Count; i++)
        {
            var w = equipManager.Slot[i].GetComponent<Weapon>();
            if (w != null && w.weaponSO != null)
            {
                weaponSOName[i] = $"{w.weaponSO.Name}(Lv{w.curLevel})";
            }
            else
            {
                weaponSOName[i] = "null";
            }
        }
    }
}
