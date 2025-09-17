using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponLibrary", menuName = "Game/Weapon Library")]
public class WeaponLibrary : ScriptableObject
{
    private WeaponData weaponData;
    [System.Serializable]
    public struct WeaponEntry
    {
        public WeaponIndex Index;
        public GameObject prefab;
        public WeaponSO weaponSO;
    }

    public List<WeaponEntry> weapons;

    private Dictionary<WeaponIndex, GameObject> prefabDict;
    private Dictionary<WeaponIndex, WeaponSO> weaponDict;

    private void OnEnable()
    {
        prefabDict = new Dictionary<WeaponIndex, GameObject>();
        foreach (var entry in weapons)
            prefabDict[entry.Index] = entry.prefab;

        weaponDict = new Dictionary<WeaponIndex, WeaponSO>();
        foreach (var entry in weapons)
            weaponDict[entry.Index] = entry.weaponSO;

    }

    public GameObject GetPrefab(WeaponIndex index)
    {
        return prefabDict.TryGetValue(index, out var prefab) ? prefab : null;
    }

    public WeaponSO GetSO(WeaponIndex index)
    {
        return weaponDict.TryGetValue(index, out var weaponSO) ? weaponSO : null;
        
    }
}
