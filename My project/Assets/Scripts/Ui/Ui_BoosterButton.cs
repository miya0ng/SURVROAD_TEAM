using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Ui_BoosterButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Focus (Progress)")]
    [SerializeField] private Slider focusSlider;         // 자식 Focus 슬라이더(값=쿨다운 진행)
    [SerializeField] private CanvasGroup canvasGroup;    // 없으면 자동 GetComponent

    [Header("Turbo Specs")]
    [SerializeField] private PlayerStatusEffects status; // 플레이어 상태 참조

    [Header("Click Hit Area")]
    [Tooltip("클릭을 받을 Graphic (투명 Image 가능). 비우면 현재 오브젝트의 Graphic 시도")]
    [SerializeField] private Graphic hitArea;            // 반드시 raycastTarget=true 여야 클릭 들어옴

    [Header("Visuals")]
    [Tooltip("아이템 기본 이미지(아이콘). 터보 '활성 중'에만 보였다가, 지속시간 종료 시 자동으로 꺼짐")]
    [SerializeField] private GameObject defaultImage;
    [SerializeField] private GameObject iconFocus;

    bool unlocked;

    public bool IsReady =>
        unlocked && focusSlider &&
        Mathf.Approximately(focusSlider.value, focusSlider.maxValue);

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        // 클릭 히트 영역 확보 시도
        if (!hitArea) hitArea = GetComponent<Graphic>();
        if (!hitArea)
            Debug.LogWarning("[Ui_BoosterButton] 클릭을 받기 위한 Graphic(hitArea)이 없습니다. 투명 Image 하나 추가하세요.");

        // 초기 비활성
        SetClickable(false);

        if (focusSlider)
        {
            focusSlider.minValue = 0f;
            focusSlider.maxValue = 1f;
            focusSlider.value = 0f;

            // 슬라이더가 클릭을 가로채지 않게 방어
            focusSlider.interactable = false;
            if (focusSlider.targetGraphic) focusSlider.targetGraphic.raycastTarget = false;

            // (권장) 슬라이더 하위 모든 Graphic의 raycast도 꺼버림
            var graphics = focusSlider.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics) g.raycastTarget = false;
        }

        // 기본 이미지는 처음엔 숨김
        if (defaultImage) defaultImage.SetActive(false);
    }

    void Start()
    {
        if (status != null)
        {
            status.OnTurboCooldownChanged += HandleTurboUI;
            status.OnTurboActiveChanged += HandleTurboActive;

            // 시작 시점 즉시 반영(아이템을 이미 먹었을 수도 있으니)
            float v, m; status.GetTurboProgress(out v, out m);
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
        // 아이템을 한 번이라도 먹으면(Unlock) → UI 사용 가능 상태 진입
        unlocked = true;

        if (focusSlider)
        {
            // "초 단위 그대로" 사용 (요구사항: value==max면 클릭 가능)
            focusSlider.minValue = 0f;
            focusSlider.maxValue = max;
            focusSlider.value = value;
        }

        SetClickable(ready); 
        if (iconFocus) iconFocus.SetActive(ready);
    }

    void HandleTurboActive(bool isActive)
    {
        // 활성 상태 동안만 기본 이미지 노출
        if (defaultImage) defaultImage.SetActive(isActive);

        if (!isActive)
        {
            // 지속시간이 방금 끝난 시점: 클릭 잠금(쿨다운기간) 진입
            SetClickable(false);
        }
    }

    void SetClickable(bool on)
    {
        if (canvasGroup)
        {
            canvasGroup.interactable = on;   // Selectable들에 영향
            canvasGroup.blocksRaycasts = on; // 자식 Graphic으로 레이캐스트 통과 허용
        }

        // 실제 클릭을 받을 그래픽이 있어야 함
        if (hitArea) hitArea.raycastTarget = on;
    }

    // === 클릭 처리 ===
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsReady || status == null) return;

        if (status.TryUseTurbo())
        {
            // 성공 시 PlayerStatusEffects가 즉시 OnTurboActiveChanged(true)를 쏘며
            // defaultImage가 켜지고, 슬라이더는 0으로 떨어짐(쿨다운 시작)
        }
    }
}
