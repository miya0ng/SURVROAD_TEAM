using System.Linq;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
   
    private string waveCsvPath = "WaveTable";
    private int[] waveOrder; // 비우면 CSV의 ID 오름차순으로 플레이

    [Header("Spawner Binding")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Timing Overrides (선택)")]
    [SerializeField] private int coSpawnPerTick = 1;    // 웨이브 공통 override
    [SerializeField] private float tickInterval = 0.5f; // 웨이브 공통 override

    private WaveDataTable waveTable;

    public int currentWaveIndex { get; private set; }
    public float WaveTimer { get; private set; }

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waveOrder != null ? waveOrder.Length : 0;

    void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner")?.GetComponent<EnemySpawner>();

        waveTable = new WaveDataTable();
        waveTable.Load(waveCsvPath);

        // 웨이브 순서 결정
        if (waveOrder == null || waveOrder.Length == 0)
        {
            waveOrder = waveTable.GetAll().Select(w => w.ID).OrderBy(id => id).ToArray();
        }

        currentWaveIndex = -1;
    }

    void Start()
    {
        if (enemySpawner != null)
            enemySpawner.OnWaveCleared += NextWave;

        NextWave();
    }

    void Update()
    {
        WaveTimer += Time.deltaTime;
    }

    public void NextWave()
    {
        currentWaveIndex++;
        if (currentWaveIndex >= waveOrder.Length)
        {
            Debug.Log("[WaveManager] 모든 웨이브 완료");
            return;
        }

        WaveTimer = 0f;
        int waveId = waveOrder[currentWaveIndex];

        var wave = waveTable.GetWaveData(waveId);
        if (wave == null)
        {
            Debug.LogError($"[WaveManager] Wave {waveId} 없음");
            NextWave(); // 스킵
            return;
        }

        Debug.Log($"[WaveManager] Wave {waveId} 시작 — 총 {wave.TotalAmount}마리");

        // 웨이브 데이터 전달 + 스폰 시작
        enemySpawner.SetWave(wave, coSpawnPerTick, tickInterval);
    }
}
