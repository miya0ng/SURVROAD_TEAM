using UnityEngine;

public class StartButtonProxy : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Dev_Editor";

    // UI Button의 OnClick에 이 메서드를 연결하세요.
    public void OnClickStart()
    {
        AudioManager.I?.PlaySFX("ButtonDefault");

        if (SceneLoader.I == null)
        {
            Debug.LogError("[StartButtonProxy] SceneLoader.I가 없습니다. SceneLoader가 DDOL로 살아있는지 확인하세요.");
            return;
        }
        SceneLoader.I.Load(sceneToLoad);
    }
}