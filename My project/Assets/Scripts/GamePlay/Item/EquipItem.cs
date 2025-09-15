using System;
using UnityEngine;

public class EquipItem : MonoBehaviour, IItem
{
    public WeaponSO weaponSO;
    private PlayerShooter player;

    public void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<PlayerShooter>();
    }
    public void Use(GameObject go)
    {
        Debug.Log("Use");
        player.EquipWeapon(weaponSO);

        Destroy(gameObject);
    }

    private void Update()
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Use(other.gameObject);
        }
        else
        {
            return;
        }
    }
}