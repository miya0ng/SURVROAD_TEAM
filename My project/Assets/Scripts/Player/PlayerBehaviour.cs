using UnityEngine;

public class PlayerBehaviour : LivingEntity, IDamagable
{
    public GameManager gameManager;
    public PlayerController playerController;
    private Rigidbody rb;
    public Ui_Game ui_Game;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        maxHp = 100;
        curHp = maxHp;
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();

    }
    void Start()
    {
        ui_Game.SetHpBar(maxHp);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, playerController.hAxis * playerController.rotationSpeed, 0f));
        
        rb.MovePosition(rb.position + transform.forward * playerController.vAxis * playerController.curMoveSpeed * Time.fixedDeltaTime);
    }

    public override void OnDeath()
    {
        base.OnDeath();
        gameManager.GameOver();
        Destroy(gameObject);
    }


    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);

        Debug.Log($"{gameObject.name} took {damage} damage. HP: {curHp}");
        ui_Game.UpdateHpBar(curHp);
    }
}
