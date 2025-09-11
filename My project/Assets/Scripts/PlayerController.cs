using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

//public static class TagManager
//{
//    public static readonly string Player = "Player";
//}

public class PlayerController : MonoBehaviour
{
    public float vAxis;
    public float hAxis = 1f;
    public float moveSpeed = 10f;
    public float rotationSpeed = 180f;

    private bool isLeft;
    private bool isRight;
    private bool isAccel;
    private bool isBreak;

    private void Awake()
    {

    }

    public void ButtonState(UiPlayButton.ButtonType button, bool isheld)
    {
        switch(button)
        {
            case UiPlayButton.ButtonType.Left:
                isLeft = isheld;
                break;
                case UiPlayButton.ButtonType.Right:
                isRight = isheld;
                break;
            case UiPlayButton.ButtonType.Break:
                isBreak = isheld;
                break;
            case UiPlayButton.ButtonType.Accel:
                isAccel = isheld;
                break;
        }
    }

    private void Update()
    {
        if (isLeft) 
        {
            hAxis = -1f;
        }
        if (isRight)
        {
            hAxis = 1f;
        }
        if (isAccel)
        {
            vAxis = 1f;
            moveSpeed += 1f;
        }
        if (isBreak)
        {
            moveSpeed -= 2f;
            vAxis = 0f;
        }

        var curMoveSpeed = Mathf.Clamp(moveSpeed, 0f, 10f);
        moveSpeed = curMoveSpeed;
        //vAxis = Input.GetAxis("Vertical");
        //hAxis = Input.GetAxis("Horizontal");
    }
}
