using UnityEngine;

public class PauseButton : MonoBehaviour
{
    public GameObject popupGO;

    public void OnClickPauseButton()
    {
        AudioManager.I?.PlaySFX("ButtonDefault");
        var popup = popupGO.GetComponentInParent<PausePopUp>();
        if (popup && !popup.gameObject.activeSelf) popup.Open();
        else if (popup && popup.gameObject.activeSelf == false) popup.Open();
        else if (popup) popup.Open();
    }
}