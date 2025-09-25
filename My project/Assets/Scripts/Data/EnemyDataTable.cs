using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 너의 CsvTable 시스템에 맞춰 최소 어댑터만 제공.
// 실제 CSV 필드명은 기획서 예시와 동일하다고 가정.

[System.Serializable]
public struct EnemySpec
{
    public int Id;
    public string Name;
    public int SizeType;          // 1 소형, 2 중형, 3 대형
    public float MaxSpeed;
    public float Durability;      // 체력
    public float Accel;           // 가속력
    public float Handling;        // 조작성(조향 응답)
    public float CollisionDamage;
    public EnemyAttackType AttackType;
    public int AttackDamage;
    public float AttackInterval;  // 초 단위
    public string PrefabName;     // 프리팹 파일명(없으면 Name 사용)
}
[System.Serializable]
public class EnemyData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Type { get; set; }
    public float MaxSpeed { get; set; }
    public float Durability { get; set; }
    public float Acceleration { get; set; }
    public float Handling { get; set; }
    public float CollisionDamage { get; set; }
    public int AttackType { get; set; }      // enum 캐스팅
    public int AttackDamage { get; set; }
    public float AttackInterval { get; set; }
    public string PrefabName { get; set; }
}

public class EnemyDataTable : DataTable
{
    public static readonly string EnemyTableId = "EnemyCarTable";

    private readonly Dictionary<int, EnemySpec> specs = new Dictionary<int, EnemySpec>();

    public override void Load(string fileName)
    {
        specs.Clear();

        var path = string.Format(dataTablePath, fileName);
        var textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError($"Failed to load enemy table: {fileName} at path: {path}");
            return;
        }

        var records = LoadCSV<EnemyData>(textAsset.text);
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"No records found in enemy table: {fileName}");
            return;
        }

        foreach (var r in records)
        {
            if (specs.ContainsKey(r.ID))
            {
                Debug.LogWarning($"Duplicate Enemy ID detected: {r.ID}, skipping...");
                continue;
            }

            var spec = new EnemySpec
            {
                Id = r.ID,
                Name = r.Name,
                SizeType = r.Type,
                MaxSpeed = r.MaxSpeed,
                Durability = r.Durability,
                Accel = r.Acceleration,                         // 매핑 주의
                Handling = r.Handling,
                CollisionDamage = r.CollisionDamage,
                AttackType = (EnemyAttackType)r.AttackType,     // enum 캐스팅
                AttackDamage = r.AttackDamage,
                AttackInterval = r.AttackInterval,
                PrefabName = r.PrefabName
            };

            specs.Add(spec.Id, spec);
        }
    }

    public EnemySpec? GetSpec(int id)
    {
        if (specs.TryGetValue(id, out var value))
            return value;

        Debug.LogWarning($"Enemy key not found: {id}");
        return null;
    }
    public bool TryGet(int id, out EnemySpec spec) => specs.TryGetValue(id, out spec);

    public IEnumerable<EnemySpec> GetBySize(int sizeType) => specs.Values.Where(s => s.SizeType == sizeType);

    public IEnumerable<EnemySpec> GetByAttackType(EnemyAttackType type) => specs.Values.Where(s => s.AttackType == type);

    public IReadOnlyCollection<EnemySpec> GetAll() => specs.Values.ToList().AsReadOnly();
}