using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponLibrary", menuName = "Game/Weapon Library")]
public class WeaponLibrary : ScriptableObject
{
    [System.Serializable]
    public struct PrefabEntry
    {
        public PrefabIndex prefabIndex;
        public GameObject prefab;
    }

    public List<PrefabEntry> prefabs;
    private Dictionary<PrefabIndex, GameObject> prefabDict;

    private void OnEnable()
    {
        prefabDict = new Dictionary<PrefabIndex, GameObject>();
        foreach (var entry in prefabs)
            prefabDict[entry.prefabIndex] = entry.prefab;
    }

    public GameObject GetPrefab(PrefabIndex index)
    {
        return prefabDict.TryGetValue(index, out var prefab) ? prefab : null;
    }
}
