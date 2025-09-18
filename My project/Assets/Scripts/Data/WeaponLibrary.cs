using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "WeaponLibrary", menuName = "Game/Weapon Library")]
public class WeaponLibrary : ScriptableObject
{
    public List<WeaponSO> weapons;

    public WeaponSO GetSO(WeaponIndex index)
    {
        return weapons.FirstOrDefault(so =>
            so.Levels.Any(l => l.PrefabIndex == index));
    }

    public WeaponLevelData GetLevelData(WeaponIndex index, int level)
    {
        var so = GetSO(index);
        if (so != null)
        {
            var data = so.Levels.FirstOrDefault(l => l.Level == level);
            if (data != null)
                return data;
        }

        Debug.LogWarning($"Weapon not found: {index}, Lv{level}");
        return null;
    }

    public Sprite GetThumbnail(WeaponIndex index, int level)
    {
        return GetLevelData(index, level)?.ThumbNail;
    }

    public GameObject GetPrefab(WeaponIndex index, int level)
    {
        return GetLevelData(index, level)?.prefab;
    }
}
