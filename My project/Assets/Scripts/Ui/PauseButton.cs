using UnityEngine;

public class PauseButton : MonoBehaviour
{
    public GameObject PausePopUpRoot;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

public void OnClickPauseButton()
    {
        if (Time.timeScale == 1)
        {
            Time.timeScale = 0;
        }
        PausePopUpRoot.SetActive(true);
    }
}
