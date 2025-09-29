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
    protected virtual void Awake()
    {
        if (curHp <= 0f) curHp = maxHp;
    }

    public virtual void OnDamage(float damage, LivingEntity attacker)
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled || isDead) return;

        curHp -= damage;
        if (curHp <= 0f)
            Die(attacker);
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
