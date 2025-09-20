using UnityEngine;
using UnityEngine.UI;
using static Bullet;
public class PlayerBehaviour : LivingEntity, IDamagable
{
    public GameManager gameManager;
    public PlayerController playerController;
    private Rigidbody rb;
    private Ui_HpBar ui_hpBar;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        maxHp = 100;
        curHp = maxHp;
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        ui_hpBar=GetComponent<Ui_HpBar>();
        ui_hpBar.SetHpBar(maxHp);
    }
    void Start()
    {
        teamId = TeamId.Player;
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
        rb.MoveRotation(
     rb.rotation * Quaternion.Euler(0f, playerController.hAxis * playerController.rotationSpeed * Time.fixedDeltaTime, 0f)
 );

        rb.MovePosition(rb.position + transform.forward * playerController.vAxis * playerController.curMoveSpeed * Time.fixedDeltaTime);
    }

    public override void Heal(float amount)
    {
        base.Heal(amount);
    }

    protected override void Die()
    {
        base.Die();
        gameManager.GameOver();
        //Destroy(gameObject);
        gameObject.SetActive(false);
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);

        //Debug.Log($"{gameObject.name} took {damage} damage. HP: {curHp}");
        ui_hpBar.UpdateHpBar(curHp);
    }
}