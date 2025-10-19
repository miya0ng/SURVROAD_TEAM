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

    [Header("HP Thresholds")]
    [SerializeField] private float warningThreshold = 60f;
    [SerializeField] private float dangerThreshold = 30f;

    private float lastHeartbeatTime = -10f;
    [SerializeField] private float heartbeatCooldown = 1.5f;

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
        base.Awake();
        maxHp = 100;
        curHp = maxHp;
        ui_hpBar = GetComponent<Ui_Slider>();
        ui_hpBar.SetSliderUi(maxHp, maxHp);

        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        equipManager = GameObject.FindWithTag("EquipManager").GetComponent<EquipManager>();

        if (smokeWhite != null) smokeWhite.SetActive(false);
        if (smokeBlack != null) smokeBlack.SetActive(false);
    }

    protected override void OnEnable()
    {
        equipManager.OnCandidate += LevelPopUpHp;
    }
    protected override void OnDisable()
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
        UpdateDamageEffects();
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

        Haptics.Light();

        int rand = Random.Range(1, 3);
        switch (rand)
        {
            case 1:
                AudioManager.I?.PlaySFX("CarCrash00", transform.position);
                break;
            case 2:
                AudioManager.I?.PlaySFX("CarCrash01", transform.position);
                break;
        }

        isCol = true;
    }
    private void UpdateDamageEffects()
    {
        // guard
        if (smokeWhite == null || smokeBlack == null) return;

        if (curHp < dangerThreshold)
        {
            smokeBlack.SetActive(true);
            smokeWhite.SetActive(false);
        }
        else if (curHp < warningThreshold)
        {
            smokeWhite.SetActive(true);
            smokeBlack.SetActive(false);
        }
        else
        {
            smokeWhite.SetActive(false);
            smokeBlack.SetActive(false);
        }
    }

    private void OnCollisionExit(Collision collision) => isCol = false;

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        Haptics.Light();

        ui_hpBar.UpdateHpSlider(curHp);
        if (curHp < 30f)
        {
            AudioManager.I.PlaySFX("HeartBeat", count:4);
        }
        if (curHp < 60f && curHp >= 30f)
        {
            AudioManager.I.PlaySFX("FireBurning", transform.position);

            if (SmokeWhite == null || smokeWhite == null) return;
            smokeWhite.SetActive(true);
        }
        if (curHp < 30f)
        {
            if (SmokeWhite == null || SmokeBlack == null || smokeBlack == null || smokeWhite == null) return;
            smokeBlack.SetActive(true);
            smokeWhite.SetActive(false);
        }
        UpdateDamageEffects();
    }

    public void ApplyMultipliers(float durabilityMul, float maxSpeedMul, float accelerationMul)
    {
        Debug.Log($"Apply Player Multipliers: durability x{durabilityMul}");
        maxHp *= durabilityMul;
        ui_hpBar.SetSliderUi(curHp, maxHp);
    }
}