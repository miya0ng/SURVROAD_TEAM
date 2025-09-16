using System;
using UnityEngine;

public class EquipItem : MonoBehaviour, IItem
{
    private GameObject rootPlayer;

    public void Awake()
    {
        rootPlayer = GameObject.FindGameObjectWithTag("Player");
    }
    public void Use(GameObject go)
    {
        if(go == null)
        {
            Debug.Log("Item is Null");
        }
        if (rootPlayer == null)
        {
            Debug.Log("player is Null");
        }
        var equipManager = rootPlayer.GetComponentInChildren<EquipManager>();
        if (equipManager == null)
        {
            Debug.Log("equipManager is Null");
        }
        equipManager.EquipWeapon(go);
    }

    private void Update()
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("¾ÆÀÌÅÛ°ú ºÎµúÈû");
            Use(gameObject);
        }
        else
        {
            return;
        }
    }
}