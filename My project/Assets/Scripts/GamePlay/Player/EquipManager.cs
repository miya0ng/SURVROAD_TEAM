using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class EquipManager : MonoBehaviour
{
    public WeaponLibrary WeaponLibrary;
    private LivingEntity player;
    [SerializeField]
    private List<Transform> sockets = new List<Transform>();
    private List<GameObject> equipWeapons = new List<GameObject>();

    private int equipCount = 0;

    bool isEquip = false;
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

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            UnEquipWeapon();
        }
    }

    public void EquipWeapon(GameObject weapon)
    {
        weapon.SetActive(false);//Todo:destroy
        if (equipCount >= 3)
        {
            Debug.Log("장착 횟수 초과");
            return;
        }
        Debug.Log("EquipWeapon");
        var oldw = weapon.GetComponent<Weapon>();
        WeaponIndex index = oldw.weaponSO.PrefabIndex;
        var equipWeapon = Instantiate(weapon, sockets[equipCount].position, sockets[equipCount].rotation);
        equipWeapon.transform.SetParent(sockets[equipCount].transform);
        equipWeapon.SetActive(true);
        var w = equipWeapon.GetComponent<Weapon>();
        var e = equipWeapon.GetComponent<EquipItem>();
        w.Equip(player);
        e.isEquip = true;
        w.weaponSO = WeaponLibrary.GetSO(index);
        equipWeapons.Add(equipWeapon);
        equipCount++;
    }

    public void UnEquipWeapon()
    {
        Debug.Log("장착 무기 없음");
        Debug.Log(equipCount);
        if (equipCount == 0)
        {
            return;
        }
        equipCount--;
        var equipWeapon = sockets[equipCount].GetComponentInChildren<Weapon>();
        Destroy(equipWeapon.gameObject);
    }
}