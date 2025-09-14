using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("�⺻ �Ӽ�")]
    public string weaponName;

    [Header("���� ����")]
    public float damage = 10f;
    public float attackSpeed = 1f;
    public int bulletCount = 1;
    public float bulletSpeed = 60f;
    public float lifeTime = 3f;

    //[Header("Ư�� �ɼ�")]
    //public bool isPiercing = false;
    //public bool isExplosive = false;
    //public float explosionRadius = 0f;

    [Header("������ ����")]
    public GameObject bulletPrefab;
    public GameObject explosionPrefab;
}