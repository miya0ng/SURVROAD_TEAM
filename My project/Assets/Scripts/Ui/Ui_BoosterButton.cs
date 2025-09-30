
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Ui_BoosterButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Focus (Progress)")]
    [SerializeField] private Slider focusSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Turbo Specs")]
    [SerializeField] private PlayerStatusEffects status;

    [Header("Click Hit Area")]
    [Tooltip("클릭을 받을 Graphic (투명 Image 가능). 비우면 현재 오브젝트의 Graphic 시도")]
    [SerializeField] private Graphic hitArea;

    [Header("Visuals")]
    [Tooltip("아이템 기본 이미지(아이콘). 터보 '활성 중'에만 보였다가, 지속시간 종료 시 자동으로 꺼짐")]
    [SerializeField] private GameObject defaultImage;
    [SerializeField] private GameObject Active;
    bool unlocked;

    // 준비 여부는 status 판단에 맞김
    public bool IsReady =>
        unlocked && status != null && status.IsTurboReady();

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        // 클릭 히트 영역 확보 시도
        if (!hitArea) hitArea = GetComponent<Graphic>();
        if (!hitArea)
            Debug.LogWarning("[Ui_BoosterButton] 클릭을 받기 위한 Graphic(hitArea)이 없습니다.");

        SetClickable(false);

        if (focusSlider)
        {
            focusSlider.minValue = 0f;
            focusSlider.maxValue = 1f;
            focusSlider.value = 1f;

            focusSlider.interactable = false;
            if (focusSlider.targetGraphic) focusSlider.targetGraphic.raycastTarget = false;

            var graphics = focusSlider.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics) g.raycastTarget = false;
        }

        if (defaultImage) defaultImage.SetActive(true);
        if (Active) Active.SetActive(true);
       
    }

    void Start()
    {
        if (status != null)
        {
            status.OnTurboCooldownChanged += HandleTurboUI;
            status.OnTurboActiveChanged += HandleTurboActive;

            float v, m;
            status.GetTurboProgress(out v, out m);
            HandleTurboUI(v, m, status.IsTurboReady());
            HandleTurboActive(status.IsTurboActive());
        }
        else
        {
            Debug.LogWarning("[Ui_BoosterButton] PlayerStatusEffects 참조가 비어있습니다. 인스펙터에서 연결하세요.");
        }
    }

    void OnDestroy()
    {
        if (status != null)
        {
            status.OnTurboCooldownChanged -= HandleTurboUI;
            status.OnTurboActiveChanged -= HandleTurboActive;
        }
    }

    void HandleTurboUI(float value, float max, bool ready)
    {
        unlocked = true;

        if (focusSlider)
        {
            focusSlider.minValue = 0f;
            focusSlider.maxValue = 1f; // 0~1 고정
            float norm = (max > 0f) ? (value / max) : 0f;
            focusSlider.value = Mathf.Clamp01(norm);
        }

        SetClickable(ready);
    }

    void HandleTurboActive(bool isActive)
    {
        if (defaultImage && !defaultImage.activeSelf) defaultImage.SetActive(true);

        if (!isActive)
        {
            bool stillReady = status != null && status.IsTurboReady();
            SetClickable(stillReady);
            if (hitArea) hitArea.gameObject.SetActive(stillReady);
        }
    }

    void SetClickable(bool on)
    {
        if (hitArea) hitArea.raycastTarget = on;
        hitArea?.gameObject.SetActive(on);
    }

    // === 클릭 처리 ===
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsReady || status == null) return;

        if (status.TryUseTurbo())
        {
            // CarController는 OnTurboActiveChanged 이벤트로 부스터 on/off를 처리하므로
            // 중복 방지를 위해 직접 호출은 생략
            // status.gameObject.GetComponent<CarController>().SetBooster(true);
        }
    }
}