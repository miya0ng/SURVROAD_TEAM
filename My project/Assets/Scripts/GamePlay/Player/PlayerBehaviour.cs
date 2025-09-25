using UnityEngine;
using UnityEngine.UI;
using static Bullet;

public class PlayerBehaviour : LivingEntity, IDamagable
{
    public GameManager gameManager;
    private Ui_Slider ui_hpBar;

    [Header("Collision Damage")]
    [SerializeField] private float colDamage = 10f;
    [SerializeField] private LayerMask damageableLayers = ~0; // 필요시 설정(예: Enemy, Destructible)
    private bool isCol = false;

    protected override void Awake()
    {
        maxHp = 100000;
        curHp = maxHp;
        ui_hpBar = GetComponent<Ui_Slider>();
        ui_hpBar.SetSliderUi(maxHp, maxHp);
    }

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

    private void OnCollisionEnter(Collision collision)
    {
        if (isCol) return;

        if (!collision.gameObject.TryGetComponent<LivingEntity>(out var lv) || lv == null)
            return;

        if (!lv.gameObject.activeInHierarchy || lv.isDead)
            return;

        lv.OnDamage(colDamage, this);
        isCol = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isCol = false;
    }

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        ui_hpBar.UpdateHpSlider(curHp);
    }
}
