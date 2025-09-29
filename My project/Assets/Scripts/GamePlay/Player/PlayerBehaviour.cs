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

    [Header("FX")]
    [SerializeField] private Transform fxAnchor;// 없으면 null로 두기
    [SerializeField] private Transform magnetAnchor;// 없으면 null로 두기
    [SerializeField] private ParticleSystem healFxPrefab;
    [SerializeField] private ParticleSystem stunFxPrefab;
    [SerializeField] private ParticleSystem sheildFxPrefab;
    [SerializeField] private ParticleSystem overPowerFxPrefab;
    [SerializeField] private ParticleSystem magnetFxPrefab;
    protected override void Awake()
    {
        maxHp = 1000;
        curHp = maxHp;
        ui_hpBar = GetComponent<Ui_Slider>();
        ui_hpBar.SetSliderUi(maxHp, maxHp);
    }

    public override void Heal(float amount)
    {
        base.Heal(amount);
        ui_hpBar.UpdateHpSlider(curHp);

        // 힐 이펙트/사운드 재생
        PlayHealFx();
    }

    public void PlayStunItemFx()
    {
        if (!stunFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        // 이펙트 생성 + 플레이어에 붙이기
        var fx = Instantiate(stunFxPrefab, parent.position, Quaternion.identity, parent);
        fx.Play();

        // 총 재생 시간 계산 후 자동 파괴
        var main = fx.main;
        float life = main.duration;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life += main.startLifetime.constantMax;
        else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            life += main.startLifetime.constant;

        Destroy(fx.gameObject, life + 0.1f);
    }
    public void PlayReinforcedShieldItemFx()
    {
        if (!sheildFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        // 이펙트 생성 + 플레이어에 붙이기
        var fx = Instantiate(sheildFxPrefab, parent.position, Quaternion.identity, parent);
        fx.Play();

        // 총 재생 시간 계산 후 자동 파괴
        var main = fx.main;
        float life = main.duration;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life += main.startLifetime.constantMax;
        else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            life += main.startLifetime.constant;

        Destroy(fx.gameObject, life + 0.1f);
    }
    public void PlayMagnetItemFx()
    {
        if (!magnetFxPrefab) return;

        Transform parent = magnetAnchor ? magnetAnchor : transform;
        // 이펙트 생성 + 플레이어에 붙이기
        var fx = Instantiate(magnetFxPrefab, parent.position, magnetFxPrefab.transform.rotation, parent);
        fx.Play();

        // 총 재생 시간 계산 후 자동 파괴
        var main = fx.main;
        float life = main.duration;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life += main.startLifetime.constantMax;
        else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            life += main.startLifetime.constant;

        Destroy(fx.gameObject, life + 0.1f);
    }


    public void PlayHealFx()
    {
        if (!healFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        // 이펙트 생성 + 플레이어에 붙이기
        var fx = Instantiate(healFxPrefab, parent.position, Quaternion.identity, parent);
        fx.Play();

        // 총 재생 시간 계산 후 자동 파괴
        var main = fx.main;
        float life = main.duration;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life += main.startLifetime.constantMax;
        else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            life += main.startLifetime.constant;

        Destroy(fx.gameObject, life + 0.1f);
    }
    public void PlayPowerOverdriveItemFx()
    {
        if (!overPowerFxPrefab) return;

        Transform parent = fxAnchor ? fxAnchor : transform;
        // 이펙트 생성 + 플레이어에 붙이기
        var fx = Instantiate(overPowerFxPrefab, parent.position, Quaternion.identity, parent);
        fx.Play();

        // 총 재생 시간 계산 후 자동 파괴
        var main = fx.main;
        float life = main.duration;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life += main.startLifetime.constantMax;
        else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            life += main.startLifetime.constant;

        Destroy(fx.gameObject, life + 0.1f);
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
        if (!collision.gameObject.TryGetComponent<LivingEntity>(out var lv) || lv == null) return;
        if (!lv.gameObject.activeInHierarchy || lv.isDead) return;

        lv.OnDamage(colDamage, this);
        isCol = true;
    }

    private void OnCollisionExit(Collision collision) => isCol = false;

    public override void OnDamage(float damage, LivingEntity attacker)
    {
        base.OnDamage(damage, attacker);
        ui_hpBar.UpdateHpSlider(curHp);
    }
}