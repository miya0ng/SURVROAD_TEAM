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
    public IReadOnlyList<GameObject> Slot => equipWeapons;
    private int equipCount = 0;
    public event System.Action OnEquipChanged;
    bool isEquip = false;
    //Todo: manage list<transform>, ���� ������ �����ؾ���
    public void Awake()
    {
        player = GetComponentInParent<LivingEntity>();
        if (player == null)
        {
            //Debug.Log("equipmanager player is null");
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
            Debug.Log("���� Ƚ�� �ʰ�");
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

        OnEquipChanged?.Invoke();
    }

    public void UnEquipWeapon()
    {
        if (equipCount == 0)
            return;

        equipCount--;

        var equipWeapon = equipWeapons[equipCount];
        equipWeapons.RemoveAt(equipCount);

        equipWeapon.SetActive(false);
        OnEquipChanged?.Invoke();
        //var equipWeapon = sockets[equipCount].GetComponentInChildren<Weapon>();
        //equipWeapon.gameObject.SetActive(false);
        //Destroy(equipWeapon.gameObject);
    }
}