using UnityEngine;

public class SpikeEquipAdd : MonoBehaviour
{
    int count = 0;
    public GameObject[] weapons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        count = GetComponent<WeaponDriver>().CurLevel;
        Debug.Log(count);
        weapons[count-1].SetActive(true);
    }
}
