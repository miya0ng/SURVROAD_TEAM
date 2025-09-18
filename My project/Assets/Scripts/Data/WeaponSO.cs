using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Game/Weapon SO")]
public class WeaponSO : ScriptableObject
{
    [Header("기본 정보")]
    public int ID;
    public string Name;
    public int Type;
    public int Target;
    public WeaponIndex PrefabIndex;
    public Sprite ThumbNail;
    public GameObject prefab; // 무기 프리팹 참조

    [Header("레벨별 데이터")]
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

    public GameObject bulletPrefab;
    public ParticleSystem effectPrefab;
}