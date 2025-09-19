using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
public class EquipManager : MonoBehaviour
{
    public WeaponLibrary WeaponLibrary;
    private LivingEntity player;
    [SerializeField]
    private List<Transform> sockets = new List<Transform>();
    private List<GameObject> equipWeapons = new List<GameObject>();
    public List<GameObject> Slot => equipWeapons;
    private int maxEquipCount = 3;

    public event System.Action OnEquipChanged;

    public int IndexOfInternal(GameObject go) => equipWeapons.IndexOf(go);

    //Todo: manage list<transform>, 무기 여러개 장착해야함
    public void Awake()
    {
        player = GetComponentInParent<LivingEntity>();
        if (player == null)
        {
            //Debug.Log("equipmanager player is null");
        }
        //GameObject[] socketObjs = GameObject.FindGameObjectsWithTag("EquipSocket");
        //foreach (var obj in socketObjs)
        //{
        //    sockets.Add(obj.transform);
        //}
        var socketObjs = GameObject.FindGameObjectsWithTag("EquipSocket")
                           .OrderBy(o => o.name, System.StringComparer.Ordinal).ToArray();
        sockets.Clear();
        foreach (var obj in socketObjs) sockets.Add(obj.transform);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnEquipWeapon();
        }
    }

    private System.Collections.IEnumerator InvokeEquipChangedNextFrame()
    {
        yield return new WaitForEndOfFrame();
        OnEquipChanged?.Invoke();
    }

    public void EquipWeapon(WeaponSO so)
    {
        var same = equipWeapons
        .Where(go => go != null)
        .Select(go => go?.GetComponent<Weapon>())
        .FirstOrDefault(w => w != null && w.weaponSO == so);

        if (same != null)
        {
            same.LevelUp();
            //OnEquipChanged?.Invoke();
            return;
        }

        if (Slot.Count >= maxEquipCount)
        {
            Debug.Log("장착 슬롯 가득 참");
            return;
        }

        var socket = sockets[equipWeapons.Count];
        var levelData = so.Levels.FirstOrDefault(l => l.Level == 1);
        if (levelData == null || levelData.prefab == null)
        {
            Debug.LogError($"{so.Name} Lv1 Prefab 없음");
            return;
        }

        var equipWeapon = Instantiate(levelData.prefab, socket.position, socket.rotation, socket);
        var w = equipWeapon.GetComponent<Weapon>();

        w.weaponSO = so;
        w.SetLevel(1);
        w.Equip(player);

        equipWeapons.Add(equipWeapon);
        StartCoroutine(InvokeEquipChangedNextFrame());
        //OnEquipChanged?.Invoke();
    }


    public void ReplaceWeapon(int oldIndex, GameObject newObject)
    {
        if (oldIndex < 0 || oldIndex >= equipWeapons.Count)
            return;

        var oldObj = equipWeapons[oldIndex];
        equipWeapons[oldIndex] = newObject;

        if (oldObj != null)
            Destroy(oldObj);
        StartCoroutine(InvokeEquipChangedNextFrame());
        //OnEquipChanged?.Invoke();
    }
    public void UnEquipWeapon()
    {
        if (Slot.Count == 0)
            return;

        var equipWeapon = equipWeapons[Slot.Count - 1];
        equipWeapons.RemoveAt(Slot.Count - 1);

        equipWeapon.SetActive(false);
        Destroy(equipWeapon);
        StartCoroutine(InvokeEquipChangedNextFrame());
        //OnEquipChanged?.Invoke();

        //var equipWeapon = sockets[equipCount].GetComponentInChildren<Weapon>();
        //equipWeapon.gameObject.SetActive(false);
        //Destroy(equipWeapon.gameObject);
    }

    public void UnEquipWeapon(int index)
    {
        if (index < 0 || index >= equipWeapons.Count) return;

        var equipWeapon = equipWeapons[index];
        equipWeapons.RemoveAt(index);

        Destroy(equipWeapon);
        //OnEquipChanged?.Invoke();
        StartCoroutine(InvokeEquipChangedNextFrame());
    }
}