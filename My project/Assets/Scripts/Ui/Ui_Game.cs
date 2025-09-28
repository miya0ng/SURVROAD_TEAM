using System;
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
    private GameManager gameManager;

    [Header("Top HUD")]
    public TextMeshProUGUI waveCount;
    public TextMeshProUGUI specialPartText;

    public TextMeshProUGUI[] slotText;
    public Image[] slotImage;
    public Ui_Slider partsGuage;

    [Header("Wave Dots (1~N)")]
    [Tooltip("웨이브 점(1~N)의 부모. 각 자식은 Default/Clear/Current를 이름으로 가진 자식을 갖는다.")]
    public Transform waveDotsRoot;

    const string LOG = "[Ui_Game]";

    // ===== Wave Dot 구조 =====
    [Serializable]
    public struct WaveDot
    {
        public Transform root;
        public GameObject goDefault;
        public GameObject goClear;
        public GameObject goCurrent;

        public void SetDefault()
        {
            if (goDefault) goDefault.SetActive(true);
            if (goClear) goClear.SetActive(false);
            if (goCurrent) goCurrent.SetActive(false);
        }
        public void SetClear()
        {
            if (goDefault) goDefault.SetActive(false);
            if (goClear) goClear.SetActive(true);
            if (goCurrent) goCurrent.SetActive(false);
        }
        public void SetCurrent()
        {
            if (goDefault) goDefault.SetActive(false);
            if (goClear) goClear.SetActive(false);
            if (goCurrent) goCurrent.SetActive(true);
        }
        public bool IsValid =>
            root != null && (goDefault || goClear || goCurrent);
    }

    private WaveDot[] waveDots = Array.Empty<WaveDot>();
    private int lastWaveShown = -1;
    private int lastTotalWaves = -1;

    // === 캐시된 이벤트 핸들러(구독/해제 짝 맞춤용) ===
    private Action<int, int> _onWaveChangedHandler;
    private Action _onAllWavesCompletedHandler;

    void Awake()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null) equipManager = player.GetComponentInChildren<EquipManager>();

        var wmObj = GameObject.FindGameObjectWithTag("WaveManager");
        if (wmObj) waveManager = wmObj.GetComponent<WaveManager>();

        var gmObj = GameObject.FindGameObjectWithTag("GameManager");
        if (gmObj) gameManager = gmObj.GetComponent<GameManager>();

        // Wave dots 자동 바인딩
        AutoCollectWaveDots();
    }

    void OnEnable()
    {
        if (equipManager != null)
        {
            equipManager.OnEquipChanged += SetSlotInfo;
            equipManager.OnWeaponLeveled += OnWeaponLeveledRefresh;
            equipManager.OnPartsGaugeChanged += UpdatePartsGauge;
            Debug.Log($"{LOG} Subscribed to EquipManager events");

            UpdatePartsGauge(equipManager.Parts, equipManager.PartsMax);
            SetSlotInfo();
        }
        else
        {
            Debug.LogWarning($"{LOG} equipManager not found at OnEnable");
        }

        if (gameManager != null)
        {
            gameManager.OnSpecialPartChanged += UpdateSpecialPartUI;
            Debug.Log($"{LOG} Subscribed to GameManager events");
        }

        // 웨이브 이벤트 구독(캐시된 델리게이트 사용)
        if (waveManager != null)
        {
            _onWaveChangedHandler = OnWaveChangedFromManager;
            _onAllWavesCompletedHandler = OnAllWavesCompletedFromManager;

            waveManager.OnWaveChanged += _onWaveChangedHandler;
            waveManager.OnAllWavesCompleted += _onAllWavesCompletedHandler;

            // 초기 1회 강제 갱신
            ForceRefreshWaveDots();
        }
        else
        {
            Debug.LogWarning($"{LOG} waveManager not found at OnEnable");
        }
    }

    void OnDisable()
    {
        if (equipManager != null)
        {
            equipManager.OnEquipChanged -= SetSlotInfo;
            equipManager.OnWeaponLeveled -= OnWeaponLeveledRefresh;
            equipManager.OnPartsGaugeChanged -= UpdatePartsGauge;
            Debug.Log($"{LOG} Unsubscribed from EquipManager events");
        }
        if (gameManager != null)
        {
            gameManager.OnSpecialPartChanged -= UpdateSpecialPartUI;
            Debug.Log($"{LOG} Unsubscribed from GameManager events");
        }

        if (waveManager != null)
        {
            if (_onWaveChangedHandler != null) waveManager.OnWaveChanged -= _onWaveChangedHandler;
            if (_onAllWavesCompletedHandler != null) waveManager.OnAllWavesCompleted -= _onAllWavesCompletedHandler;
            _onWaveChangedHandler = null;
            _onAllWavesCompletedHandler = null;
            Debug.Log($"{LOG} Unsubscribed from WaveManager events");
        }
    }

    void Update()
    {
        // 텍스트 웨이브 카운트는 매 프레임 갱신해도 싼 작업
        if (waveCount != null && waveManager != null)
            waveCount.text = $"{waveManager.CurrentWaveNumber}";
    }

    // === Event Handlers ===
    private void OnWeaponLeveledRefresh(WeaponDriver drv)
    {
        Debug.Log($"{LOG} OnWeaponLeveled -> {drv.weaponSO?.Name} Lv{drv.CurLevel}");
        SetSlotInfo();
    }

    private void UpdatePartsGauge(float cur, float max)
    {
        if (partsGuage == null)
        {
            Debug.LogWarning($"{LOG} partsGuage is null");
            return;
        }
        partsGuage.SetSliderUi(cur, max);
    }

    private void UpdateSpecialPartUI(int count)
    {
        if (specialPartText == null) return;
        specialPartText.text = $"x {count}";
    }

    private void SetSlotInfo()
    {
        if (equipManager == null)
        {
            Debug.LogWarning($"{LOG} SetSlotInfo aborted: equipManager is null");
            return;
        }

        var infos = equipManager.GetSlotInfos();
        int n = Mathf.Min(slotImage.Length, infos.Length);

        for (int i = 0; i < slotImage.Length; i++)
        {
            if (i < n && !infos[i].IsEmpty)
            {
                slotImage[i].sprite = infos[i].Thumbnail;
                slotImage[i].gameObject.SetActive(true);
                if (i < slotText.Length) slotText[i].text = $"Lv.{infos[i].Level}";
            }
            else
            {
                slotImage[i].sprite = null;
                slotImage[i].gameObject.SetActive(false);
                if (i < slotText.Length) slotText[i].text = string.Empty;
            }
        }
        Debug.Log($"{LOG} SetSlotInfo() infos={infos.Length}, slots={slotImage.Length}");
    }

    // ===== Wave Dots =====

    private void AutoCollectWaveDots()
    {
        if (waveDotsRoot == null)
        {
            Debug.LogWarning($"{LOG} waveDotsRoot is null. Wave dots UI will not work.");
            waveDots = Array.Empty<WaveDot>();
            return;
        }

        int childCount = waveDotsRoot.childCount;
        waveDots = new WaveDot[childCount];

        for (int i = 0; i < childCount; i++)
        {
            var child = waveDotsRoot.GetChild(i);

            GameObject FindChildGO(string name)
            {
                var t = child.Find(name);
                return t ? t.gameObject : null;
            }

            waveDots[i] = new WaveDot
            {
                root = child,
                goDefault = FindChildGO("Default"),
                goClear = FindChildGO("Clear"),
                goCurrent = FindChildGO("Current")
            };

            if (!waveDots[i].IsValid)
                Debug.LogWarning($"{LOG} WaveDot[{i}] is missing Default/Clear/Current under {child.name}");
        }

        Debug.Log($"{LOG} AutoCollectWaveDots: collected {waveDots.Length} dots");
    }

    private void ForceRefreshWaveDots()
    {
        if (waveManager == null) return;
        lastWaveShown = -1; // 강제 리프레시 상태로 만들고
        lastTotalWaves = -1;
        RefreshWaveDots(Mathf.Max(1, waveManager.CurrentWaveNumber),
                        Mathf.Max(0, waveManager.TotalWaves));
        lastWaveShown = waveManager.CurrentWaveNumber;
        lastTotalWaves = waveManager.TotalWaves;
    }

    // WaveManager 이벤트 → UI 반영
    private void OnWaveChangedFromManager(int current, int total)
    {
        RefreshWaveDots(current, total);
        lastWaveShown = current;
        lastTotalWaves = total;
    }

    private void OnAllWavesCompletedFromManager()
    {
        // 모든 웨이브 완료 시, 마지막 칸을 Clear로 만들거나 Current를 유지할지 정책 선택
        // 여기서는 전부 Clear 처리
        if (waveManager != null)
        {
            RefreshWaveDots(waveManager.TotalWaves, waveManager.TotalWaves);
        }
    }

    private void RefreshWaveDots(int currentWaveNumber, int totalWaves)
    {
        if (waveDots == null || waveDots.Length == 0) return;

        int dotCount = waveDots.Length;
        int cur = Mathf.Clamp(currentWaveNumber, 1, dotCount);
        int total = Mathf.Clamp(totalWaves, 0, dotCount);

        for (int i = 0; i < dotCount; i++)
        {
            int waveIndex1Based = i + 1;
            ref var dot = ref waveDots[i];

            if (!dot.IsValid) continue;

            if (waveIndex1Based < cur)                       // 이미 지난 웨이브
            {
                dot.SetClear();
            }
            else if (waveIndex1Based == cur && waveIndex1Based <= total) // 현재 웨이브
            {
                dot.SetCurrent();
            }
            else                                             // 아직 도달 전
            {
                dot.SetDefault();
            }
        }

        // 디버그/동기 텍스트 갱신(선택)
        if (waveCount != null) waveCount.text = $"{cur}";
        // Debug.Log($"{LOG} RefreshWaveDots -> cur:{cur} / total:{total}");
    }
}
