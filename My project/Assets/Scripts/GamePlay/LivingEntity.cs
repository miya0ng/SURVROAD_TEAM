using System;
using UnityEngine;
using static Bullet;
public class LivingEntity : MonoBehaviour, IDamagable
{
    public float maxHp;
    public float curHp;
    public Action onDeath;

    public TeamId teamId;
    public virtual void OnDamage(float damage, LivingEntity attacker)
    {
        curHp -= damage;
        //Debug.Log($"{gameObject.name} took {damage} damage. HP: {curHp}");

        if (curHp <= 0)
            Die();
    }

    public virtual void Heal(float amount)
    {
        curHp += amount;
        if (curHp >= maxHp)
        {
            curHp = maxHp;
        }
    }


    protected virtual void Die()
    {
       Debug.Log($"=={gameObject.name} + isdead!==");
       onDeath?.Invoke();
    }
}