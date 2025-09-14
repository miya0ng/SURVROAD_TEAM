using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("기본 속성")]
    public string weaponName;

    [Header("공격 스펙")]
    public float damage = 10f;
    public float attackSpeed = 1f;
    public int bulletCount = 1;
    public float bulletSpeed = 60f;
    public float lifeTime = 3f;

    //[Header("특수 옵션")]
    //public bool isPiercing = false;
    //public bool isExplosive = false;
    //public float explosionRadius = 0f;

    [Header("프리팹 참조")]
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;
}