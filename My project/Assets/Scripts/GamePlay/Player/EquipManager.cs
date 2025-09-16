using NUnit.Framework;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
public class EquipManager : MonoBehaviour
{
    private LivingEntity player;
    private List<Transform> sockets = new List<Transform> ();
    //Todo: manage list<transform>, 무기 여러개 장착해야함
    public void Awake()
    {
        player = GameObject.FindGameObjectWithTag("PlayerMe").GetComponent<LivingEntity>();
        if(player == null)
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
        //var w = weapon.GetComponent<Weapon>();
        //w.Equip(player);
        var equipWeapon = Instantiate(weapon, sockets[0].position, Quaternion.identity);
        weapon.SetActive(false);//Todo:
    }
}