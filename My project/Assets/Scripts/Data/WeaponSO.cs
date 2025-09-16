using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data")]
public class WeaponSO : ScriptableObject
{
    [Header("TYPE")]
    public int ID;
    public string Name;
    public int Type;
    public int Target;
    public int Level;

    [Header("SPEC")]
    public float Damage;
    public int ShotCount;
    public float AttackSpeed;
    public float AttackRange;
    public float Dps;
    public float BulletSpeed;
    public float EffectiveRange;
    public float ExplosionRange;
    public float Duration;
    public bool Piercing;

    [Header("INFO")]
    public string Info;
    public string PrefabName; //weaponPrefab
    public WeaponIndex PrefabIndex;

}