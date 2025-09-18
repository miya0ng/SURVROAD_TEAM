using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Game/Weapon SO")]
public class WeaponSO : ScriptableObject
{
    [Header("�⺻ ����")]
    public int ID;
    public string Name;
    public int Type;
    public int Target;
    public WeaponIndex PrefabIndex;
    public Sprite ThumbNail;
    public GameObject prefab; // ���� ������ ����

    [Header("������ ������")]
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