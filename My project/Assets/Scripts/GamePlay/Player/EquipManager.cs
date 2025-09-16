using NUnit.Framework;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using static UnityEditor.PlayerSettings;
public class EquipManager : MonoBehaviour
{
    public WeaponLibrary WeaponLibrary;
    private LivingEntity player;
    [SerializeField]
    private List<Transform> sockets = new List<Transform>();
    private List<GameObject> equipWeapons = new List<GameObject>();

    private int equipCount = 0;

    //Todo: manage list<transform>, 무기 여러개 장착해야함
    public void Awake()
    {
        player = GetComponentInParent<LivingEntity>();
        if (player == null)
        {
            Debug.Log("equipmanager player is null");
        }
        GameObject[] socketObjs = GameObject.FindGameObjectsWithTag("EquipSocket");
        foreach (var obj in socketObjs)
        {
            sockets.Add(obj.transform);
        }
    }

    public void EquipWeapon(GameObject weapon)
    {
        //if (equipCount >= 3)
        //{
        //    Debug.Log("장착 횟수 초과");
        //    return;
        //}
    
        var oldw = weapon.GetComponent<Weapon>();
        WeaponIndex index = oldw.weaponSO.PrefabIndex;
        var equipWeapon = Instantiate(weapon, sockets[0].position, sockets[0].rotation);
        weapon.SetActive(false);//Todo:destroy

        var w = equipWeapon.GetComponent<Weapon>();
        w.Equip(player);
        w.weaponSO = WeaponLibrary.GetSO(index);
        equipWeapons.Add(equipWeapon);
        equipCount++;
    }
}