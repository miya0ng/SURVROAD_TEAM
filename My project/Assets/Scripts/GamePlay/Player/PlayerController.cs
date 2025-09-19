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

    public float curMoveSpeed {  get; set; }
    public float rotationSpeed { get; set; }

    private float maxSpeed = 40f;
    private float maxReverseSpeed = 20f;
    private float acceleration = 10f;
    private float deceleration = 10f;
    private float reverseAccel = 10f;

    private bool isLeft;
    private bool isRight;
    private bool isAccel;
    private bool isBreak;

    public void Awake()
    {
        curMoveSpeed = 100f;
        rotationSpeed = 150f;
    }
    public void ButtonState(UiPlayButton.ButtonType button, bool isHeld)
    {
        switch (button)
        {
            case UiPlayButton.ButtonType.Left:
                isLeft = isHeld;
                break;
            case UiPlayButton.ButtonType.Right:
                isRight = isHeld;
                break;
            case UiPlayButton.ButtonType.Accel:
                isAccel = isHeld;
                break;
            case UiPlayButton.ButtonType.Break:
                isBreak = isHeld;
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isLeft || Input.GetKey(KeyCode.A))
            hAxis = -1f;
        else if (isRight || Input.GetKey(KeyCode.D))
            hAxis = 1f;
        else
            hAxis = 0f;


        if (isAccel || Input.GetKey(KeyCode.W))
        {
            vAxis = 1f;
            curMoveSpeed += acceleration * Time.fixedDeltaTime;
        }

        else if (isBreak || Input.GetKey(KeyCode.S))
        {
            if (curMoveSpeed > 0f && vAxis > 0f)
            {
                curMoveSpeed -= deceleration * Time.fixedDeltaTime;
                if (curMoveSpeed <= 0f)
                {
                    curMoveSpeed = 0f;
                    vAxis = -1f;
                }
            }
            else
            {
                vAxis = -1f;
                curMoveSpeed += reverseAccel * Time.fixedDeltaTime;
            }
        }
        else
        {
            if (curMoveSpeed > 0f)
            {
                curMoveSpeed -= deceleration * Time.fixedDeltaTime;
                if (curMoveSpeed < 0f) curMoveSpeed = 0f;
            }
        }

        if (vAxis > 0f)
            curMoveSpeed = Mathf.Clamp(curMoveSpeed, 0f, maxSpeed);
        else if (vAxis < 0f)
            curMoveSpeed = Mathf.Clamp(curMoveSpeed, 0f, maxReverseSpeed);
    }
}