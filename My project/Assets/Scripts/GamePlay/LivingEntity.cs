using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamagable
{
    [Header("HP")]
    public float maxHp = 100f;
    public float curHp;

    [Header("Team")]
    public TeamId teamId;

    public Action<LivingEntity> onDeath;
    public bool isDead = false;

    [Header("Ref")]
    public GameObject SmokeWhite = null;
    public GameObject SmokeBlack = null;

    public GameObject smokeWhite = null;
    public GameObject smokeBlack = null;

    protected virtual void Awake()
    {
        if (curHp <= 0f) curHp = maxHp;

        if (SmokeWhite != null || SmokeBlack != null)
        {
            smokeWhite = Instantiate(SmokeWhite);
            smokeWhite.transform.SetParent(transform);
            smokeWhite.SetActive(false);

            smokeBlack = Instantiate(SmokeBlack);
            smokeBlack.transform.SetParent(transform);
            smokeBlack.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        if (curHp <= 0f) curHp = maxHp;
        isDead = false;

        if (SmokeWhite != null && smokeWhite == null)
        {
            smokeWhite = Instantiate(SmokeWhite, transform);
            smokeWhite.SetActive(false);
        }

        if (SmokeBlack != null && smokeBlack == null)
        {
            smokeBlack = Instantiate(SmokeBlack, transform);
            smokeBlack.SetActive(false);
        }
    }

    protected virtual void OnDisable()
    {
        if (smokeWhite != null) smokeWhite.SetActive(false);
        if (smokeBlack != null) smokeBlack.SetActive(false);
    }

    public virtual void OnDamage(float damage, LivingEntity attacker)
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled || isDead) return;

        curHp -= damage;

        if (curHp <= 0f)
        {
            curHp = 0;
            if (smokeBlack != null)
                Destroy(smokeBlack);
            Die(attacker);
        }

        if (curHp < 60f && curHp >= 30f)
        {
            // AudioManager.I.PlaySFX("FireBurning", transform.position);

            if (SmokeWhite == null || smokeWhite == null) return;
            smokeWhite.SetActive(true);
        }
        if (curHp < 30f)
        {
            if (SmokeWhite == null || SmokeBlack == null || smokeBlack == null || smokeWhite == null) return;
            smokeBlack.SetActive(true);
            smokeWhite.SetActive(false);
        }

    }

    public virtual void Heal(float amount)
    {
        curHp = Mathf.Min(curHp + amount, maxHp);
    }

    protected virtual void Die(LivingEntity killer = null)
    {
        // Debug.Log($"== {gameObject.name} is dead ==");
        isDead = true;
        onDeath?.Invoke(this);
    }
}
