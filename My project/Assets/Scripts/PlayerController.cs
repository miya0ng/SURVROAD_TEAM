using UnityEngine;
using UnityEngine.InputSystem;

//public static class TagManager
//{
//    public static readonly string Player = "Player";
//}

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;

    private float vAxis;
    private float hAxis;
    private Rigidbody rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        vAxis = Input.GetAxis("Vertical");
        hAxis = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        Debug.Log(vAxis);
        Debug.Log(hAxis);
        // 회전
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, hAxis * rotationSpeed * Time.fixedDeltaTime, 0f));

        // 이동
        rb.MovePosition(rb.position + transform.forward * vAxis * moveSpeed * Time.fixedDeltaTime);

    }
}
