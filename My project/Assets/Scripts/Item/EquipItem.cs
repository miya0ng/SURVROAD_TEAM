using System;
using UnityEngine;

public class EquipItem : MonoBehaviour, IItem
{
    public WeaponData weaponData;
    public enum Types
    {
        auto_turret_lv1,
        auto_turret_lv2
    }

    public Types itemType;
    public void Use(GameObject go)
    {
        Debug.Log("Use");
        switch (itemType)
        {
            case Types.auto_turret_lv1:
                break;
            case Types.auto_turret_lv2:
                break;
            default:
                break;
        }
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