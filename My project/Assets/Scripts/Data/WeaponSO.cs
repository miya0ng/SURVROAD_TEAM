using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Game/Weapon SO")]
public class WeaponSO : ScriptableObject
{
    [Header("Info")]
    public int ID;
    public string Name;
    public int Kind;
    public int Type;
    public int Target;

    [Header("Level Data")]
    public List<WeaponLevelData> Levels;
}

[System.Serializable]
public class WeaponLevelData
{
    public int Level;
    public float MinDamage;
    public float MaxDamage;
    public int ShotCount;
    public float AttackSpeed;
    public float AttackRange;
    public float BulletSpeed;
    public float EffectiveRange;
    public float ExplosionRange;
    public float Duration;
    public bool Piercing;
    public string Info;

    public WeaponIndex PrefabIndex;
    public Sprite ThumbNail;
    public GameObject prefab; // 무기 프리팹 참조
    public GameObject bulletPrefab;
    public ParticleSystem effectPrefab;
}