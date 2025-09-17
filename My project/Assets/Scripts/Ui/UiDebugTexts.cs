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
    public TextMeshProUGUI LeftEnemy;

    private PlayerController player;
    private PlayerBehaviour playerHp;
    private WaveManager waveManager;
    private EnemySpawner enemySpawner;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
        playerHp = GameObject.FindWithTag("Player").GetComponent<PlayerBehaviour>();
        waveManager = GameObject.FindWithTag("WaveManager").GetComponent<WaveManager>();
        enemySpawner = GameObject.FindWithTag("EnemySpawner").GetComponent<EnemySpawner>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        weaponName.text = "WeaponName: ";
        speed.text = "Speed: " + player.curMoveSpeed;
        hp.text = "Hp: " + playerHp.curHp + "/" + playerHp.maxHp;

        waveCount.text = "WaveCount: " + waveManager.currentWave;
        timePerWave.text = $"TimePerWave: {waveManager.WaveTimer:F2}";
        LeftEnemy.text = "LeftEnemy: " + enemySpawner.ActiveEnemyCount + "/" + enemySpawner.waveSpawnCount;
    }
}