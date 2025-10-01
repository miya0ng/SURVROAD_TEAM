using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader I;

    [Header("Prefab with Canvas + Slider + TMP Text")]
    [SerializeField] private GameObject loadingCanvas;   // 프리팹 할당 (필수)

    // 인스턴스에서 가져올 실제 레퍼런스(런타임에 채움)
    private GameObject canvasInstance;
    private Slider progressBar;
    private TextMeshProUGUI progressText;

    [Header("Tags (set these in Inspector & on the child objects)")]
    [SerializeField] private string sliderTag = "LoadingProgressBar";
    [SerializeField] private string percentTextTag = "LoadingPercentText";

    [Header("Options")]
    [SerializeField] private float minShowTime = 0.3f;
    [SerializeField] private float smoothSpeed = 2.0f;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        Time.timeScale = 1f;
        DontDestroyOnLoad(gameObject);
    }

    //public void OnClickStart()
    //{
    //    Load("Dev_Editor");
    //}

    public void Load(string sceneName)
    {
        Time.timeScale = 1f;
        StartCoroutine(CoLoad(sceneName));
    }

    private IEnumerator CoLoad(string sceneName)
    {
        // 1) 로딩 캔버스 인스턴스 준비
        if (canvasInstance == null)
        {
            if (loadingCanvas == null)
            {
                Debug.LogError("[SceneLoader] loadingCanvas(prefab)가 비어있습니다.");
                yield break;
            }

            canvasInstance = Instantiate(loadingCanvas);
            DontDestroyOnLoad(canvasInstance);

            // --- 태그로 자식에서 찾기 ---
            var sliderGO = FindInChildrenByTag(canvasInstance, sliderTag);
            if (sliderGO) progressBar = sliderGO.GetComponent<Slider>();
            var percentGO = FindInChildrenByTag(canvasInstance, percentTextTag);
            if (percentGO) progressText = percentGO.GetComponent<TextMeshProUGUI>();

            if (progressBar == null) Debug.LogError("[SceneLoader] 태그로 Slider를 찾지 못했습니다.");
            if (progressText == null) Debug.LogError("[SceneLoader] 태그로 TextMeshProUGUI를 찾지 못했습니다.");

            // 슬라이더 안전 세팅
            if (progressBar)
            {
                progressBar.minValue = 0f;
                progressBar.maxValue = 1f;
                progressBar.wholeNumbers = false;
                progressBar.value = 0f;
            }
        }

        canvasInstance.SetActive(true);
        yield return null; // 레이아웃 1프레임

        // 2) 비동기 로딩 시작
        var op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] 씬 로드 실패: {sceneName}");
            yield break;
        }
        op.allowSceneActivation = false;

        float shown = 0f;  // 텍스트 표시(보간용)
        float t = 0f;

        while (!op.isDone)
        {
            // 0~0.9 → 0~1 정규화된 실제 진행도
            float norm = Mathf.Clamp01(op.progress / 0.9f);

            // 슬라이더는 "실제 진행도"를 바로 사용 → 즉각 반응
            if (progressBar) progressBar.value = norm;

            // 텍스트는 부드럽게
            shown = Mathf.MoveTowards(shown, norm, smoothSpeed * Time.unscaledDeltaTime);
            if (progressText) progressText.text = $"{shown * 100f:0}%";

            t += Time.unscaledDeltaTime;

            // 로딩 완료 + 최소 노출 시간 후 씬 활성화
            if (norm >= 1f && t >= minShowTime)
                op.allowSceneActivation = true;

            yield return null;
        }

        yield return new WaitForEndOfFrame();
        if (canvasInstance) canvasInstance.SetActive(false);
    }

    // 자식 트리에서 Tag로 오브젝트 찾기(비활성 포함)
    private GameObject FindInChildrenByTag(GameObject root, string tag)
    {
        if (root == null || string.IsNullOrEmpty(tag)) return null;
        var trs = root.GetComponentsInChildren<Transform>(true);
        foreach (var tr in trs)
        {
            if (tr.CompareTag(tag)) return tr.gameObject;
        }
        return null;
    }
}
