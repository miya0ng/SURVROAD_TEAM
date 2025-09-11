using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    private PlayerController playerController;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();

    }
    void Start()
    {
        bool voo;
        voo = true;
        voo = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, playerController.hAxis * playerController.rotationSpeed * Time.fixedDeltaTime, 0f));
        rb.MovePosition(rb.position + transform.forward * playerController.vAxis * playerController.moveSpeed * Time.fixedDeltaTime);
    }
}
