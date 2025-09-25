using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyCarData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Type { get; set; }
    public int MaxSpeed { get; set; }
    public int Durability { get; set; }
    public int Acceleration { get; set; }
    public int Handling { get; set; }
    public int CollisionDamage { get; set; }
    public int AttackType { get; set; }    // 1:Rush,2:Shooter,3:Suicide,4:Heavy(¿¹½Ã)
    public int AttackDamage { get; set; }
    public int AttackInterval { get; set; }    // sec
    public string Info { get; set; }
    public string PrefabName { get; set; }
}

public class EnemyCarDataTable : DataTable
{
    public static readonly string EnemyCarTableId = "EnemyCarTable";

    private Dictionary<int, EnemyCarData> cars = new Dictionary<int, EnemyCarData>();

    public override void Load(string fileName)
    {
        cars.Clear();

        var path = string.Format(dataTablePath, fileName);
        var textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError($"Failed to load enemy car table: {fileName} at path: {path}");
            return;
        }

        var records = LoadCSV<EnemyCarData>(textAsset.text);
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"No records found in enemy car table: {fileName}");
            return;
        }

        cars = records
            .Where(r => r != null && r.ID > 0)
            .GroupBy(r => r.ID)
            .Select(g => g.First())
            .ToDictionary(r => r.ID, r => r);
    }

    public EnemyCarData GetEnemyCarData(int key)
    {
        if (cars.TryGetValue(key, out var value))
            return value;

        Debug.LogWarning($"EnemyCar key not found: {key}");
        return null;
    }

    public IEnumerable<EnemyCarData> GetAll() => cars.Values;
    public IEnumerable<EnemyCarData> FindByType(int type) => cars.Values.Where(c => c.Type == type);
}
