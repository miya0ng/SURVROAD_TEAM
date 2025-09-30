using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleScene : MonoBehaviour
{
    [SerializeField] string sceneName = "Loading"; // 로드할 씬 이름

    public void LoadTargetScene()
    {
        Time.timeScale = 1f; // 혹시 일시정지 상태면 해제
        SceneManager.LoadScene(sceneName);
    }
}
