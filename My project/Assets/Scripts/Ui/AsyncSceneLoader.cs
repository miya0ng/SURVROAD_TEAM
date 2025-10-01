using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AsyncSceneLoader : MonoBehaviour
{
    [SerializeField] string sceneName = "Dev_Editor";
    [SerializeField] Slider progressBar; // 선택(없어도 됨)
    public TextMeshProUGUI progressText; // 선택(없어도 됨)

    private void Awake()
    {

    }

    public void OnEnable()
    {
        if (progressBar) progressBar.value = 0f;
        if (progressText) progressText.text = "0%";
        LoadAsync();
    }
    public void LoadAsync()
    {
        Time.timeScale = 1f;
        StartCoroutine(CoLoad());
    }

    IEnumerator CoLoad()
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // 0~0.9f 까지 진행됨(유니티 규칙)
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = p;
            progressText.text = $"{p * 100f:0}%";
            if (p >= 1f)
                op.allowSceneActivation = true;

            yield return null;
        }
    }
}
