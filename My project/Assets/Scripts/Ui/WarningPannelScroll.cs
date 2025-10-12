using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WarningPanelScroll_CameraBound : MonoBehaviour
{
    [Header("Parent Group (타일들의 공통 부모)")]
    [SerializeField] private RectTransform group;

    [Header("BG Arrow Tiles (좌→우 순 권장, 개수 자유)")]
    [SerializeField] private List<RectTransform> tiles = new();
    [SerializeField] private float speed = -120f;   // px/sec (음수=왼쪽)

    [Header("Text Tiles (씬에 미리 2개 배치해서 넣기)")]
    [SerializeField] private List<RectTransform> textTiles = new(); // 최소 2개
    [SerializeField] private float textSpeed = -220f;

    [Header("Glow (선택)")]
    [SerializeField] private CanvasGroup glowGroup;
    [SerializeField] private float glowMin = 0.15f;
    [SerializeField] private float glowMax = 0.55f;
    [SerializeField] private float glowPeriod = 3.0f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("General")]
    [SerializeField] private bool useUnscaledTime = true;

    // 내부 상태
    Canvas rootCanvas;
    Camera uiCam;             // Overlay면 null
    Vector2 camLeftRight;     // (leftX, rightX) in group local space
    float tPulse;

    void Reset()
    {
        group = transform as RectTransform;
        glowGroup = GetComponentInChildren<CanvasGroup>();
    }

    void Awake()
    {
        if (!group) group = transform as RectTransform;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas)
            uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        UpdateCameraBounds();

        // 시작 시 좌표 기준 정렬(선택)
        if (tiles.Count > 0) tiles.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
        if (textTiles.Count > 0) textTiles.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));

        // 텍스트 2개가 화면 안에서 바로 이어지도록 첫 프레임 정렬(안 넣어도 동작하지만 깔끔)
        if (textTiles.Count >= 2)
        {
            var left = textTiles[0];
            var right = textTiles[1];
            // 왼쪽 타일을 화면 왼쪽 경계에, 오른쪽 타일을 그 뒤에 이어붙이기
            SetLeft(left, camLeftRight.x);
            SetLeft(right, RightEdge(left));
        }
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        UpdateCameraBounds();

        // 1) 배경 타일 스크롤 & 루프
        if (tiles.Count > 0) ScrollAndLoop(tiles, speed * dt);

        // 2) 텍스트 타일 스크롤 & 루프
        if (textTiles.Count > 0) ScrollAndLoop(textTiles, textSpeed * dt);

        // 3) Glow Pulse
        if (glowGroup)
        {
            tPulse += dt / Mathf.Max(0.0001f, glowPeriod);
            float p = tPulse % 1f;
            float yoyo = 1f - Mathf.Abs(p * 2f - 1f);
            glowGroup.alpha = Mathf.Lerp(glowMin, glowMax, pulseCurve.Evaluate(yoyo));
        }
    }

    // --- 공통 스크롤/루프 로직 (컨베이어) ---
    void ScrollAndLoop(List<RectTransform> list, float delta)
    {
        // 이동
        for (int i = 0; i < list.Count; i++)
            list[i].anchoredPosition += new Vector2(delta, 0f);

        // 정렬 후 좌/우 끝 참조
        list.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
        var left = list[0];
        var right = list[list.Count - 1];

        if (delta < 0f) // 왼쪽으로 흐름
        {
            // 가장 왼쪽 타일이 화면 왼쪽 바깥으로 완전히 나가면 → 맨 오른쪽 뒤에 이어붙임
            if (RightEdge(left) <= camLeftRight.x)
            {
                float newLeft = RightEdge(right);
                SetLeft(left, newLeft);
            }
        }
        else if (delta > 0f) // 오른쪽으로 흐름
        {
            // 가장 오른쪽 타일이 화면 오른쪽 바깥으로 완전히 나가면 → 맨 왼쪽 앞에 붙임
            if (LeftEdge(right) >= camLeftRight.y)
            {
                float newRight = LeftEdge(left);
                SetRight(right, newRight);
            }
        }
    }

    // --- 카메라(스크린) 경계를 group 로컬로 변환 ---
    void UpdateCameraBounds()
    {
        if (!group) return;

        Vector2 screenL = new Vector2(0f, Screen.height * 0.5f);
        Vector2 screenR = new Vector2(Screen.width, Screen.height * 0.5f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(group, screenL, uiCam, out Vector2 leftLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(group, screenR, uiCam, out Vector2 rightLocal);

        camLeftRight = new Vector2(leftLocal.x, rightLocal.x);
    }

    // --- 좌/우 끝 계산 & 배치 보조 ---
    float Width(RectTransform rt) => rt.rect.width * rt.localScale.x;

    float LeftEdge(RectTransform rt) => rt.anchoredPosition.x - rt.pivot.x * Width(rt);
    float RightEdge(RectTransform rt) => rt.anchoredPosition.x + (1f - rt.pivot.x) * Width(rt);

    void SetLeft(RectTransform rt, float leftX)
    {
        float w = Width(rt);
        rt.anchoredPosition = new Vector2(leftX + rt.pivot.x * w, rt.anchoredPosition.y);
    }
    void SetRight(RectTransform rt, float rightX)
    {
        float w = Width(rt);
        rt.anchoredPosition = new Vector2(rightX - (1f - rt.pivot.x) * w, rt.anchoredPosition.y);
    }
}