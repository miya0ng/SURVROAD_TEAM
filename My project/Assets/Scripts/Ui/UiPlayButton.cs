using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiPlayButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isHeld;
    private PlayerController playerController;

    public ButtonType buttonType = ButtonType.Accel;
    public enum ButtonType
    {
        Left,
        Right,
        Accel,
        Break
    }

    public void Awake()
    {
        playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
    }
    public void Update()
    {
        playerController.ButtonState(buttonType, isHeld);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
    }
}