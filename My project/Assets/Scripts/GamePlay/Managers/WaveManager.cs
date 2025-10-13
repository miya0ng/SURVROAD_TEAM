using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // public AudioClip waveStartSFX;
    public AudioClip waveMainBGM;

    private string waveCsvPath = "WaveTable";
    private int[] waveOrder; // 비우면 CSV의 ID 오름차순으로 플레이

    private EnemySpawner enemySpawner;
    private GameManager gameManager;

    [Header("Timing Overrides (선택)")]
    [SerializeField] private int coSpawnPerTick = 1;    // 웨이브 공통 override
    [SerializeField] private float tickInterval = 0.5f; // 웨이브 공통 override

    private WaveDataTable waveTable;

    // === Public State ===
    public int currentWaveIndex { get; private set; } = -1;
    public float WaveTimer { get; private set; }

    public int CurrentWaveNumber => currentWaveIndex + 1;               // 1-based
    public int TotalWaves => waveOrder != null ? waveOrder.Length : 0;  // 전체 웨이브 수

    // === Events for UI ===
    /// <summary>웨이브가 바뀌었을 때 (현재 웨이브 번호 1-based, 총 웨이브 수)</summary>
    public event Action<int, int> OnWaveChanged;
    /// <summary>모든 웨이브 완료</summary>
    public event Action OnAllWavesCompleted;

    public float warningPanelDuration = 5.0f; // wave duration to start

    void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = GameObject.FindGameObjectWithTag("EnemySpawner")?.GetComponent<EnemySpawner>();
        if (gameManager == null)
            gameManager = GameObject.FindGameObjectWithTag("GameManager")?.GetComponent<GameManager>();

        waveTable = new WaveDataTable();
        waveTable.Load(waveCsvPath);

        // 웨이브 순서 결정
        if (waveOrder == null || waveOrder.Length == 0)
        {
            waveOrder = waveTable.GetAll().Select(w => w.ID).OrderBy(id => id).ToArray();
        }
    }

    IEnumerator Start()
    {
        if (enemySpawner != null)
            enemySpawner.OnWaveCleared += NextWave;
        if (gameManager != null)
            OnAllWavesCompleted += gameManager.StageClear;

        // 씬 전환 직후 한 프레임 대기 (DontDestroyOnLoad 싱글톤/믹서 준비시간 확보)
        yield return null;

        // 안전 가드 & 실제 재생 (한 번만)
        if (AudioManager.I == null) { Debug.LogError("[WaveManager] AudioManager가 없습니다."); }
        else if (!waveMainBGM) { Debug.LogError("[WaveManager] waveMainBGM 미할당"); }
        else
        {
            AudioManager.I.PlayBGM(waveMainBGM);
        }

        NextWave(); // 첫 웨이브 시작
    }

    void OnDestroy()
    {
        if (enemySpawner != null)
            enemySpawner.OnWaveCleared -= NextWave;
    }

    void Update()
    {
        WaveTimer += Time.deltaTime;
    }

    /// <summary>다음 웨이브로 진행</summary>
    public void NextWave()
    {
        // 다음 인덱스
        currentWaveIndex++;

        if (waveOrder == null || waveOrder.Length == 0)
        {
            Debug.LogWarning("[WaveManager] waveOrder가 비어있습니다.");
            AllWavesCompleted();
            return;
        }

        if (currentWaveIndex >= waveOrder.Length)
        {
            Debug.Log("[WaveManager] 모든 웨이브 완료");
            AllWavesCompleted();
            return;
        }

        WaveTimer = 0f;
        int waveId = waveOrder[currentWaveIndex];

        var wave = waveTable.GetWaveData(waveId);
        if (wave == null)
        {
            Debug.LogError($"[WaveManager] Wave {waveId} 없음, 스킵");
            NextWave(); // 스킵하고 다음으로
            return;
        }

        Debug.Log($"[WaveManager] Wave {waveId} 시작 — 총 {wave.TotalAmount}마리");

        // UI에 현재/총 웨이브 알림 (1-based)
        OnWaveChanged?.Invoke(CurrentWaveNumber, TotalWaves);

        // 웨이브 데이터 전달 + 스폰 시작
        if (enemySpawner != null)
        {
            enemySpawner.SetWave(wave, coSpawnPerTick, tickInterval);
        }
        else
        {
            Debug.LogWarning("[WaveManager] enemySpawner가 없어 웨이브를 시작할 수 없습니다.");
        }
    }

    private void AllWavesCompleted()
    {
        OnAllWavesCompleted?.Invoke();
    }

    // --- 유틸(선택) ---
    /// <summary>현재 웨이브가 마지막인지</summary>
    public bool IsLastWave() => currentWaveIndex >= 0 && currentWaveIndex == TotalWaves - 1;

    /// <summary>지정 웨이브(1-based)로 강제 점프 (디버그/스킵용)</summary>
    public void JumpToWave(int waveNumber1Based)
    {
        if (waveOrder == null || waveOrder.Length == 0) return;
        int idx = Mathf.Clamp(waveNumber1Based - 1, 0, waveOrder.Length - 1);
        currentWaveIndex = idx - 1; // NextWave()가 +1 하므로 한 칸 이전으로 두고 호출
        NextWave();
    }

    /// <summary>웨이브 전체 초기화 후 1웨이브부터 재시작</summary>
    public void RestartAllWaves()
    {
        currentWaveIndex = -1;
        NextWave();
    }
}
