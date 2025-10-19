using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("Audio UI")]
    //[SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Header("Other UI")]
    [SerializeField] Toggle vibrationToggle;
    [SerializeField] Toggle lowToggle;
    [SerializeField] Toggle mediumToggle;
    [SerializeField] Toggle highToggle;
    [SerializeField] Button saveButton;
    [SerializeField] Button resetButton;
    [SerializeField] Button exitButton;
    [SerializeField] GameObject setting;

    // PlayerPrefs keys
    const string K_MASTER = "vol_master";
    const string K_BGM = "vol_bgm";
    const string K_SFX = "vol_sfx";
    const string K_VIBE = "vibration";
    const string K_QUAL = "quality";

    void Awake()
    {
        // defaults
        float master = PlayerPrefs.GetFloat(K_MASTER, 0.8f);
        float bgm = PlayerPrefs.GetFloat(K_BGM, 0.8f);
        float sfx = PlayerPrefs.GetFloat(K_SFX, 0.8f);
        bool vibe = PlayerPrefs.GetInt(K_VIBE, 1) == 1;
        int qual = PlayerPrefs.GetInt(K_QUAL, QualitySettings.GetQualityLevel());

        // UI set without events
       // if (masterSlider) masterSlider.SetValueWithoutNotify(master);
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(bgm);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfx);
        if (vibrationToggle) vibrationToggle.SetIsOnWithoutNotify(vibe);
        SetQualityToggles(qual);

        // apply to systems
        ApplyAudio(master, bgm, sfx);
        ApplyVibration(vibe);
        ApplyQuality(qual);

        // wire events
        //if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        if (vibrationToggle) vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
        if (lowToggle) lowToggle.onValueChanged.AddListener(v => { if (v) OnQualityPicked(0); });
        if (mediumToggle) mediumToggle.onValueChanged.AddListener(v => { if (v) OnQualityPicked(1); });
        if (highToggle) highToggle.onValueChanged.AddListener(v => { if (v) OnQualityPicked(2); });

        if (saveButton) saveButton.onClick.AddListener(SavePrefs);
        if (resetButton) resetButton.onClick.AddListener(ResetToDefault);
        if (exitButton) exitButton.onClick.AddListener(OnExit);
    }

    //void OnMasterChanged(float v)
    //{
    //    AudioManager.I?.SetMasterVolume(v);
    //    PlayerPrefs.SetFloat(K_MASTER, v);
    //    AudioManager.I?.PlaySFX("ui_click");
    //}

    private void OnExit()
    {
        setting.SetActive(false);
        AudioManager.I?.PlaySFX("ButtonDefault");
    }
    void OnBgmChanged(float v)
    {
        AudioManager.I?.SetBgmVolume(v);
        PlayerPrefs.SetFloat(K_BGM, v);
        AudioManager.I?.PlaySFX("ButtonDefault");
    }

    void OnSfxChanged(float v)
    {
        AudioManager.I?.SetSfxVolume(v);
        PlayerPrefs.SetFloat(K_SFX, v);
        AudioManager.I?.PlaySFX("ButtonDefault");
    }

    void OnVibrationChanged(bool on)
    {
        ApplyVibration(on);
        PlayerPrefs.SetInt(K_VIBE, on ? 1 : 0);
    }

    void OnQualityPicked(int idx)
    {
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        idx = Mathf.Clamp(idx, 0, max);
        ApplyQuality(idx);
        PlayerPrefs.SetInt(K_QUAL, idx);
    }

    void ApplyAudio(float master, float bgm, float sfx)
    {
        var am = AudioManager.I;
        if (am == null) return;
        am.SetMasterVolume(master);
        am.SetBgmVolume(bgm);
        am.SetSfxVolume(sfx);
    }

    void ApplyVibration(bool on)
    {
#if UNITY_ANDROID || UNITY_IOS
        // 시스템 진동 설정 연동이 필요하면 여기서 처리
        // 예: Handheld.Vibrate()는 즉시 진동만 지원. 토글만 저장.

        Haptics.Enabled = on;
#endif
    }

    void ApplyQuality(int idx)
    {
        // 적용 (현재 코드와 호환)
        QualitySettings.SetQualityLevel(idx, true);

        // 그림자 관련 런타임 강제 설정 (빌트인 렌더러용)
        if (idx >= 2) // 예: idx 2 이상을 '높음'으로 간주
        {
            QualitySettings.shadows = ShadowQuality.All;          // 그림자 켜기 (Soft + Hard)
            QualitySettings.shadowDistance = 80f;                 // 그림자 최대 거리
            QualitySettings.shadowCascades = 4;                   // 캐스케이드 수
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
        }
        else
        {
            QualitySettings.shadows = ShadowQuality.HardOnly;     // 낮은 레벨은 단색 그림자
            QualitySettings.shadowDistance = 30f;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowProjection = ShadowProjection.CloseFit;
        }

        SetQualityToggles(idx);
    }

    void SetQualityToggles(int idx)
    {
        if (lowToggle) lowToggle.SetIsOnWithoutNotify(idx == 0);
        if (mediumToggle) mediumToggle.SetIsOnWithoutNotify(idx == 1);
        if (highToggle) highToggle.SetIsOnWithoutNotify(idx >= 2);
    }

    void SavePrefs()
    {
        PlayerPrefs.Save();
        AudioManager.I?.PlaySFX("ButtonDefault");
        setting.SetActive(false);
    }
    void ResetToDefault()
    {
       // if (masterSlider) masterSlider.value = 0.8f;
        if (bgmSlider) bgmSlider.value = 0.8f;
        if (sfxSlider) sfxSlider.value = 0.8f;
        if (vibrationToggle) vibrationToggle.isOn = true;
        OnQualityPicked(0);
        SavePrefs();
    }
}