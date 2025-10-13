using UnityEngine;
using UnityEngine.UI;
using static Bullet;
using static EquipManager;

public class PlayerBehaviour : LivingEntity, IDamagable, IPlayerUpgradable
{
    private GameManager gameManager;
    private EquipManager equipManager;

    private Ui_Slider ui_hpBar;

    [Header("Collision Damage")]
    [SerializeField] private float colDamage = 10f;
    private bool isCol = false;

    [Header("FX")]
    [SerializeField] private Transform fxAnchor;
    [SerializeField] private Transform magnetAnchor;
    [SerializeField] private ParticleSystem healFxPrefab;
    [SerializeField] private ParticleSystem stunFxPrefab;
    [SerializeField] private ParticleSystem sheildFxPrefab;
    [SerializeField] private ParticleSystem overPowerFxPrefab;
    [SerializeField] private ParticleSystem magnetFxPrefab;
    protected override void Awake()
    {
        maxHp = 100;
        curHp = maxHp;
        ui_hpBar = GetComponent<Ui_Slider>();
        ui_hpBar.SetSliderUi(maxHp, maxHp);

        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        equipManager = GameObject.FindWithTag("EquipManager").GetComponent<EquipManager>();
    }

    public void OnEnable()
    {
        equipManager.OnCandidate += LevelPopUpHp;
    }
    public void OnDisable()
    {
        equipManager.OnCandidate -= LevelPopUpHp;
    }


    public void LevelPopUpHp()
    {
        curHp *= 1.3f;
        if (curHp > maxHp) curHp = maxHp;
        ui_hpBar.UpdateHpSlider(curHp);
    }
    public override void Heal(float amount)
    {
        base.Heal(amount);
        ui_hpBar.UpdateHpSlider(curHp);

        PlayHealFx();
    }

    public void PlayStunItemFx()
    {
        if (!stunFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        var fx = Instantiate(stunFxPrefab, parent.position, stunFxPrefab.transform.rotation, parent);
        fx.Play();

        float totalDuration = fx.main.duration + fx.main.startLifetime.constantMax;

        Destroy(fx.gameObject, totalDuration);
    }
    public void PlayReinforcedShieldItemFx()
    {
        if (!sheildFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        var fx = Instantiate(sheildFxPrefab, parent.position, sheildFxPrefab.transform.rotation, parent);
        fx.Play();

        float totalDuration = fx.main.duration + fx.main.startLifetime.constantMax;

        Destroy(fx.gameObject, totalDuration);
    }
    public void PlayMagnetItemFx()
    {
        if (!magnetFxPrefab) return;

        Transform parent = magnetAnchor ? magnetAnchor : transform;
        var fx = Instantiate(magnetFxPrefab, parent.position, magnetFxPrefab.transform.rotation, parent);
        fx.Play();

        float totalDuration = fx.main.duration + fx.main.startLifetime.constantMax;

        Destroy(fx.gameObject, totalDuration);
    }


    public void PlayHealFx()
    {
        if (!healFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        var fx = Instantiate(healFxPrefab, parent.position, healFxPrefab.transform.rotation, parent);
        fx.Play();

        float totalDuration = fx.main.duration + fx.main.startLifetime.constantMax;

        Destroy(fx.gameObject, totalDuration);
    }
    public void PlayPowerOverdriveItemFx()
    {
        if (!overPowerFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        var fx = Instantiate(overPowerFxPrefab, parent.position, overPowerFxPrefab.transform.rotation, parent);
        fx.Play();

        float totalDuration = fx.main.duration + fx.main.startLifetime.constantMax;

        Destroy(fx.gameObject, totalDuration);
    }

    protected override void Die(LivingEntity killer)
    {
        base.Die();
        gameManager.GameOver();
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isCol) return;
        if (!collision.gameObject.TryGetComponent<LivingEntity>(out var lv) || lv == null) return;
        if (!lv.gameObject.activeInHierarchy || lv.isDead) return;

        lv.OnDamage(colDamage, this);
        AudioManager.I.PlaySFX("CarCrash00", transform.position);

        isCol = true;
    }

    private void OnCollisionExit(Collision collision) => isCol = false;

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        ui_hpBar.UpdateHpSlider(curHp);
        if (curHp < 30f)
        {
            AudioManager.I.PlaySFX("HeartBeat", count:3);
        }
    }

    public void ApplyMultipliers(float durabilityMul, float maxSpeedMul, float accelerationMul)
    {
        Debug.Log($"Apply Player Multipliers: durability x{durabilityMul}");
        maxHp *= durabilityMul;
        ui_hpBar.SetSliderUi(curHp, maxHp);
    }
}