// Assets/Scripts/UI/ResultPopup.cs
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EquipManager equipManager; // Player 하위의 EquipManager

     private GameManager gameManager;
     private WaveManager waveManager;

    [Header("Weapons (max 3)")]
    [SerializeField] private Image[] weaponIcons = new Image[3];
    [SerializeField] private TextMeshProUGUI[] weaponLvTexts = new TextMeshProUGUI[3];
    [SerializeField] private Sprite unknownSprite;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;

    [Header("PopUpGameObject")]
    [SerializeField] private GameObject clear;
    [SerializeField] private GameObject fail;

    [Header("ClearPopUpRef")]
    [SerializeField] private TextMeshProUGUI killCount;
    [SerializeField] private TextMeshProUGUI playTime;

    private bool wired;

    void Awake()
    {
        clear.SetActive(false);
        fail.SetActive(false);

        if (!gameManager) gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        if (!waveManager) waveManager = GameObject.FindWithTag("WaveManager").GetComponent<WaveManager>();

        if (!equipManager)
        {
            var player = GameObject.FindWithTag("Player");
            if (player) equipManager = player.GetComponentInChildren<EquipManager>();
        }
    }

    void OnEnable()
    {
        WireEvents(true);
        if (restartButton) restartButton.onClick.AddListener(OnClickRestart);
    }

    void OnDisable()
    {
        WireEvents(false);
        if (restartButton) restartButton.onClick.RemoveListener(OnClickRestart);
    }

    private void WireEvents(bool on)
    {
        if (!gameManager) return;

        if (on && !wired)
        {
            gameManager.OnGameOver += HandleGameOver;
            wired = true;
        }
        else if (!on && wired)
        {
            gameManager.OnGameOver -= HandleGameOver;
            wired = false;
        }
    }

    private void HandleGameOver(bool cleared)
    {
        FillWeaponsFromEquipManager();
        fail.SetActive(!cleared);
        clear.SetActive(cleared);
        var count = 97;
        killCount.text = $"KILL {count} ENEMIES";
        var ts = System.TimeSpan.FromSeconds(gameManager.playTime);
        playTime.text = ts.ToString(@"mm\:ss\:fff");
    }

    private void FillWeaponsFromEquipManager()
    {
        // 초기화
        for (int i = 0; i < weaponIcons.Length; i++)
        {
            if (weaponIcons[i]) weaponIcons[i].sprite = unknownSprite;
            if (weaponLvTexts != null && i < weaponLvTexts.Length && weaponLvTexts[i])
                weaponLvTexts[i].text = "Lv -";
        }

        if (!equipManager) return;

        var infos = equipManager.GetSlotInfos(); // 최대 maxEquipCount 길이
        int n = Mathf.Min(infos.Length, weaponIcons.Length);

        for (int i = 0; i < n; i++)
        {
            var info = infos[i];

            if (weaponIcons[i])
                weaponIcons[i].sprite = info.IsEmpty ? unknownSprite : (info.Thumbnail ? info.Thumbnail : unknownSprite);

            if (weaponLvTexts != null && i < weaponLvTexts.Length && weaponLvTexts[i])
                weaponLvTexts[i].text = info.IsEmpty ? "Lv -" : $"Lv {Mathf.Max(info.Level, 1)}";
        }
    }

    public void OnClickRestart()
    {
        AudioManager.I?.PlaySFX("ButtonDefault");
        gameManager?.GameStart();
    }

    public void OnExitButton()
    {
        AudioManager.I?.PlaySFX("ButtonDefault");
        Time.timeScale = 1f;
        fail.SetActive(false);
        SceneLoader.I?.Load("Title");
    }
}
