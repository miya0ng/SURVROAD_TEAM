using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Type { get; set; }
    public int Target { get; set; }
    public int Level { get; set; }
    public float MinDamage { get; set; }
    public float MaxDamage { get; set; }
    public int ShotCount { get; set; }
    public float AttackSpeed { get; set; }
    public float AttackRange { get; set; }
    public float BulletSpeed {  get; set; }
    public float EffectiveRange { get; set; }
    public float ExplosionRange { get; set;}
    public float Duration {  get; set; }
    public bool Piercing {  get; set; }
    public string Info { get; set; }
    public string PrefabName { get; set; }
}
public class WeaponDataTable : DataTable
{
    public static readonly string WeaponTableId = "TestWeapon";

    private Dictionary<int, WeaponData> weapons = new Dictionary<int, WeaponData>();
    public override void Load(string fileName)
    {
        weapons.Clear();
        var path = string.Format(dataTablePath, fileName);
        var textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError($"Failed to load string table: {fileName} at path: {path}");
            return;
        }

        var records = LoadCSV<WeaponData>(textAsset.text);
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"No records found in string table: {fileName}");
            return;
        }

        weapons = records.ToDictionary(r => r.ID, r => r);
    }

    public WeaponData GetWeaponData(int key)
    {
        if (weapons.TryGetValue(key, out var value))
        {
            return value;
        }
        Debug.LogWarning($"Item key not found: {key}");
        return null;
    }
}