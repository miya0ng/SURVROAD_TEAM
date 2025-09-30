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

    readonly List<GameObject> _focuses = new();

    private void Start()
    {
        if (resumeBtn)
            resumeBtn.onClick.AddListener(OnClickResume);
        if (titleBtn)
            titleBtn.onClick.AddListener(OnClickTitle);
        if (restartBtn)
            restartBtn.onClick.AddListener(OnClickRestart);
    }

    // === 버튼 동작 ===
    public void OnClickResume()
    {
        Debug.Log("Resume Clicked");
        Time.timeScale = 1f;
        if (pauseRoot) pauseRoot.SetActive(false);
    }

    public void OnClickTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnClickRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
