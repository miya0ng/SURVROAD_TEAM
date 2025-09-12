using System;
using UnityEngine;

public interface IDamagable
{
    void OnDamage(float damage, LivingEntity attacker = null);
}