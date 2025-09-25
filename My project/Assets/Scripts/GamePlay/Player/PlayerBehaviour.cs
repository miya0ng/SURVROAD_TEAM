using UnityEngine;
using UnityEngine.UI;
using static Bullet;
public class PlayerBehaviour : LivingEntity, IDamagable
{
    public GameManager gameManager;
    private Ui_Slider ui_hpBar;


    private float colDamage = 10;
    private bool isCol = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    protected override void Awake()
    {
        maxHp = 100000;
        curHp = maxHp;
        ui_hpBar = GetComponent<Ui_Slider>();
        ui_hpBar.SetSliderUi(maxHp, maxHp);
    }
    // Update is called once per frame

    public override void Heal(float amount)
    {
        base.Heal(amount);
        ui_hpBar.UpdateHpSlider(curHp);
    }

    protected override void Die(LivingEntity killer)
    {
        base.Die();
        gameManager.GameOver();
        Destroy(gameObject);
        gameObject.SetActive(false);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (isCol) return;
        var lv = collision.gameObject.GetComponent<LivingEntity>();
        lv.OnDamage(colDamage, this);
        isCol = true;
    }

    public void OnCollisionExit(Collision collision)
    {
        isCol = false;
    }
    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);

        //Debug.Log($"{gameObject.name} took {damage} damage. HP: {curHp}");
        ui_hpBar.UpdateHpSlider(curHp);
    }
}