using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePopUp : MonoBehaviour
{
    [Header("Menu Root")]
    [SerializeField] private GameObject pauseRoot;     // 일시정지 UI 최상위

    [Header("Buttons (for focus manage)")]
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button titleBtn;
    [SerializeField] private Button restartBtn;

    [Header("Title Scene")]
    [SerializeField] private string titleSceneName = "Title";

    public void Open()
    {
        Time.timeScale = 0f;
        if (pauseRoot) pauseRoot.SetActive(true);
        gameObject.SetActive(true);
    }

    public void Close()
    {
        Time.timeScale = 1f;
        if (pauseRoot) pauseRoot.SetActive(false);
    }
    void Start()
    {
        // 버튼 바인딩
        resumeBtn?.onClick.AddListener(Close);
        titleBtn?.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(titleSceneName);
        });
        restartBtn?.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }
    // === 버튼 동작 ===
    public void OnClickResume()
    {
        AudioManager.I.PlaySFX("ButtonDefault");
        Debug.Log("Resume Clicked");
        Time.timeScale = 1f;
        if (pauseRoot) pauseRoot.SetActive(false);
    }

    public void OnClickTitle()
    {
        AudioManager.I.PlaySFX("ButtonDefault");
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnClickRestart()
    {
        AudioManager.I.PlaySFX("ButtonDefault");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
