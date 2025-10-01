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

    [Header("Mount Rules")]
    public MountPolicy MountPolicy = MountPolicy.Single;
    public SocketMask Allowed = SocketMask.Top;
    public SocketType[] PreferenceOrder;
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
    public string SelectionInfo;

    public WeaponIndex PrefabIndex;
    public Sprite ThumbNail;
    public GameObject prefab; // ¹«±â ÇÁ¸®ÆÕ
    public GameObject bulletPrefab;
    public ParticleSystem effectPrefab;
}
