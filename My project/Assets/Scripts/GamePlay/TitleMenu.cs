// Assets/Scripts/UI/TitleMenuAsync.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleMenu: MonoBehaviour
{
    public AudioClip TitleBGM;

    [SerializeField] private string gameSceneName = "Game";
    [Header("Loading UI (옵션)")]
    [SerializeField] private CanvasGroup loadingGroup;
    [SerializeField] private Slider progressBar;


    private void Start()
    {
        AudioManager.I?.PlayBGM(TitleBGM);
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        if (loadingGroup) ShowLoading(true);

        var op = SceneManager.LoadSceneAsync(gameSceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // 유니티 특성상 progress는 0~0.9까지 올라감
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = p;

            // 준비 완료되면 씬 활성화
            if (op.progress >= 0.9f)
            {
                // 필요 시 잠깐 대기/페이드 등
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }

    private void ShowLoading(bool on)
    {
        loadingGroup.alpha = on ? 1f : 0f;
        loadingGroup.blocksRaycasts = on;
        loadingGroup.interactable = on;
        loadingGroup.gameObject.SetActive(on);
    }
}
