using System;
using UnityEngine;

public class EquipItem : MonoBehaviour, IItem
{
    public WeaponSO weaponSO;

    private GameObject rootPlayer;
    public bool isEquip {  get; set; }
    public void Awake()
    {
        rootPlayer = GameObject.FindGameObjectWithTag("Player");
    }
    public void Use(GameObject player)
    {
        var equipManager = player.GetComponentInChildren<EquipManager>();
        if (equipManager == null) return;
        equipManager.EquipWeapon(weaponSO);
    }

    private void Update()
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isEquip)
        {
            isEquip = true;
            Debug.Log("¾ÆÀÌÅÛ°ú ºÎµúÈû");
            Use(other.gameObject);
        }
        else
        {
            return;
        }
    }
}