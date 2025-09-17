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
    public TextMeshProUGUI weaponSO;

    private GameObject player;
    private PlayerController playerController;
    private PlayerBehaviour playerHp;
    private WaveManager waveManager;
    private EnemySpawner enemySpawner;
    private EquipManager equipManager;

    private string[] weaponSOName = new string[3];
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerHp = player.GetComponent<PlayerBehaviour>();
        equipManager = player.GetComponentInChildren<EquipManager>();
        waveManager = GameObject.FindWithTag("WaveManager").GetComponent<WaveManager>();
        enemySpawner = GameObject.FindWithTag("EnemySpawner").GetComponent<EnemySpawner>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(player == null || playerController == null || playerHp == null || weaponSO == null || waveManager == null || enemySpawner == null)
        {
            return;
        }
        weaponName.text = "WeaponName: ";
        speed.text = "Speed: " + playerController.curMoveSpeed;
        hp.text = "Hp: " + playerHp.curHp + "/" + playerHp.maxHp;

        waveCount.text = "WaveCount: " + waveManager.currentWave;
        timePerWave.text = $"TimePerWave: {waveManager.WaveTimer:F2}";
        leftEnemy.text = "LeftEnemy: " + enemySpawner.ActiveEnemyCount + "/" + enemySpawner.waveSpawnCount;
        if (equipManager.Slot.Count != 0)
        {
            FindWeaponSOName();
        }
        weaponSO.text = "WeaponSO: \n(1)" + weaponSOName[0] + "\n(2)" + weaponSOName[1] + "\n(3)" + weaponSOName[2];
    }

    private void FindWeaponSOName()
    {
        if(equipManager.Slot.Count == 0)
        {
            return;
        }
        for(int i = 0; i< equipManager.Slot.Count; i++)
        {
            var w = equipManager.Slot[i].GetComponent<Weapon>();
            weaponSOName[i] = w.weaponSO.Name;
        }
    }
}