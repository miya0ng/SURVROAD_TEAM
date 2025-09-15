using System.Threading;
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

    public float curMoveSpeed = 30f;
    public float rotationSpeed = 150f;

    private float maxSpeed = 40f;
    private float maxReverseSpeed = 20f;
    private float acceleration = 1f;
    private float deceleration = 10f;
    private float reverseAccel = 10f;

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

    private void FixedUpdate()
    {
        if (isLeft)
        {
            hAxis -= 1f * Time.fixedDeltaTime;
        }
        else if (isRight)
        {
            hAxis += 1f * Time.fixedDeltaTime;
        }
        else
        {
            hAxis = 0f;
        }

        if (isAccel)
        {
            vAxis = 1f;
            curMoveSpeed += (curMoveSpeed + acceleration) * Time.fixedDeltaTime;
        }
        else if (isBreak)
        {   
            curMoveSpeed -= reverseAccel * Time.fixedDeltaTime;
        }

        else
        {
            if (curMoveSpeed > 0f)
                curMoveSpeed -= deceleration * Time.fixedDeltaTime;
            else if (curMoveSpeed < 0f)
                curMoveSpeed += deceleration * Time.fixedDeltaTime;
        }

        // 속도 제한
        curMoveSpeed = Mathf.Clamp(curMoveSpeed, -maxReverseSpeed, maxSpeed);
        var newMoveSpeed = Mathf.Clamp(curMoveSpeed, 0f, maxSpeed);
        curMoveSpeed = newMoveSpeed;

        //vAxis = Input.GetAxis("Vertical");
        //hAxis = Input.GetAxis("Horizontal");
    }
}