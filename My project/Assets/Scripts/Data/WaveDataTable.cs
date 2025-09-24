using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class WaveData
{
    public int ID { get; set; }
    public string Name { get; set; }

    // 최대 3세트 지원 (필요 시 더 늘릴 수 있음)

    public int EnemyID_1 { get; set; }
    public int Amount_1 { get; set; }

    public int EnemyID_2 { get; set; }
    public int Amount_2 { get; set; }
    public int EnemyID_3 { get; set; }
    public int Amount_3 { get; set; }

    public string Info { get; set; }

    // === 편의 프로퍼티/메서드 ===
    public int TotalAmount =>
        GetEnemySlots().Sum(s => s.amount);

    public IEnumerable<(int enemyID, int amount)> GetEnemySlots()
    {
        if (EnemyID_1 > 0 && Amount_1 > 0) yield return (EnemyID_1, Amount_1);
        if (EnemyID_2 > 0 && Amount_2 > 0) yield return (EnemyID_2, Amount_2);
        if (EnemyID_3 > 0 && Amount_3 > 0) yield return (EnemyID_3, Amount_3);
    }
}

public class WaveDataTable : DataTable
{
    public static readonly string WaveTableId = "WaveTable";

    private readonly Dictionary<int, WaveData> waves = new Dictionary<int, WaveData>();

    public override void Load(string fileName)
    {
        waves.Clear();

        var path = string.Format(dataTablePath, fileName);
        var textAsset = Resources.Load<TextAsset>(path);

        if (textAsset == null)
        {
            Debug.LogError($"Failed to load wave table: {fileName} at path: {path}");
            return;
        }

        var records = LoadCSV<WaveData>(textAsset.text);
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning($"No records found in wave table: {fileName}");
            return;
        }

        foreach (var rec in records)
        {
            if (rec == null || rec.ID <= 0) continue;

            if (waves.ContainsKey(rec.ID))
            {
                Debug.LogWarning($"Duplicate Wave ID detected: {rec.ID}, skipping...");
                continue;
            }
            waves.Add(rec.ID, rec);
        }
    }

    public WaveData GetWaveData(int id)
    {
        if (waves.TryGetValue(id, out var value))
            return value;

        Debug.LogWarning($"Wave ID not found: {id}");
        return null;
    }

    public IEnumerable<WaveData> GetAll() => waves.Values.OrderBy(w => w.ID);

    // === 4) 스폰 계획 헬퍼(스포너 연동용) ===
    public static IEnumerable<(int enemyID, int batchCount)> BuildBatches(
        WaveData wave,
        int coSpawnCountPerTick = 1
    )
    {
        if (wave == null) yield break;

        // 남은 수량 테이블(EnemyID, 남은 마릿수)
        var remain = wave.GetEnemySlots()
                         .Select(s => new { enemyID = s.enemyID, count = s.amount })
                         .ToList();

        if (remain.Count == 0) yield break;

        int idx = 0;
        while (remain.Any(r => r.count > 0))
        {
            if (idx >= remain.Count) idx = 0;

            var r = remain[idx];
            if (r.count > 0)
            {
                int spawnNow = Mathf.Min(coSpawnCountPerTick, r.count);
                yield return (r.enemyID, spawnNow);
                remain[idx] = new { r.enemyID, count = r.count - spawnNow };
            }

            idx++;
        }
    }
}