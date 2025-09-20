using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ItemData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public ItemType Type { get; set; }
    public int EffectType { get; set; }
    public int DropPoint { get; set; }
    public float DropRate { get; set; }
    public int UseEffect { get; set; }
    public int Duration { get; set; }
    public int Damage { get; set; }
    public string Info { get; set; }
    public string PrefabName { get; set; }
}

public class ItemDataTable : DataTable
{
    public static readonly string ItemTableId = "ItemTable";

    private Dictionary<int, ItemData> items = new Dictionary<int, ItemData>();

    public override void Load(string fileName)
    {
        items.Clear();
        var path = string.Format(dataTablePath, fileName);
        var textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError($"Failed to load item table: {fileName} at path: {path}");
            return;
        }

        var records = LoadCSV<ItemData>(textAsset.text);
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"No records found in item table: {fileName}");
            return;
        }

        foreach (var rec in records)
        {
            if (items.ContainsKey(rec.ID))
            {
                Debug.LogWarning($"Duplicate Item ID detected: {rec.ID}, skipping...");
                continue;
            }

            items.Add(rec.ID, rec);
        }
    }

    public ItemData GetItemData(int key)
    {
        if (items.TryGetValue(key, out var value))
            return value;

        Debug.LogWarning($"Item key not found: {key}");
        return null;
    }

    public IEnumerable<ItemData> GetItemsByDropPoint(int dropPoint)
    {
        return items.Values.Where(i => i.DropPoint == dropPoint);
    }
}