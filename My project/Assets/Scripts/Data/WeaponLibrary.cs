using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponLibrary", menuName = "Game/Weapon Library")]
public class WeaponLibrary : ScriptableObject
{
    public List<WeaponSO> weapons;

    public WeaponSO GetSO(WeaponIndex index)
    {
        return weapons.Find(w => w.PrefabIndex == index);
    }

    public WeaponLevelData GetLevelData(WeaponIndex index, int level)
    {
        var so = GetSO(index);
        if (so != null)
            return so.Levels.Find(l => l.Level == level);

        Debug.LogWarning($"Weapon not found: {index}, Lv{level}");
        return null;
    }

    public Sprite GetThumbnail(WeaponIndex index)
    {
        return GetSO(index)?.ThumbNail;
    }

    public GameObject GetPrefab(WeaponIndex index)
    {
        return GetSO(index)?.prefab;
    }
}