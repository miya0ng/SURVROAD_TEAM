#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WeaponSOImporter : EditorWindow
{
    private TextAsset csvFile;

    [MenuItem("Tools/Import WeaponSO From CSV (Grouped)")]
    public static void ShowWindow()
    {
        GetWindow<WeaponSOImporter>("WeaponSO Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV → WeaponSO 변환 (레벨 그룹)", EditorStyles.boldLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);

        if (GUILayout.Button("Import") && csvFile != null)
        {
            Import(csvFile);
        }
    }

    private void Import(TextAsset csv)
    {
        var records = DataTable.LoadCSV<WeaponData>(csv.text);
        if (records == null || records.Count == 0)
        {
            Debug.LogWarning("CSV 비어있음");
            return;
        }

        string root = "Assets";
        string dataSoFolder = "Assets/DataSO";
        string folderPath = "Assets/DataSO/Weapons";

        if (!AssetDatabase.IsValidFolder(dataSoFolder))
            AssetDatabase.CreateFolder(root, "DataSO");

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(dataSoFolder, "Weapons");

        var grouped = records.GroupBy(r => r.ID);

        foreach (var group in grouped)
        {
            var first = group.First();

            var so = ScriptableObject.CreateInstance<WeaponSO>();
            so.ID = first.ID;
            so.Name = first.Name;
            so.Type = first.Type;
            so.Target = first.Target;

            if (!string.IsNullOrEmpty(first.PrefabName) &&
                System.Enum.TryParse(first.PrefabName, out WeaponIndex index))
            {
                so.PrefabIndex = index;
            }

            so.Levels = new List<WeaponLevelData>();
            foreach (var record in group)
            {
                var levelData = new WeaponLevelData
                {
                    Level = record.Level,
                    MinDamage = record.MinDamage,
                    MaxDamage = record.MaxDamage,
                    ShotCount = record.ShotCount,
                    AttackSpeed = record.AttackSpeed,
                    AttackRange = record.AttackRange,
                    BulletSpeed = record.BulletSpeed,
                    EffectiveRange = record.EffectiveRange,
                    ExplosionRange = record.ExplosionRange,
                    Duration = record.Duration,
                    Piercing = record.Piercing,
                    Info = record.Info
                };

                so.Levels.Add(levelData);
            }

            string assetPath = $"{folderPath}/{first.ID}_{first.Name}.asset";
            AssetDatabase.CreateAsset(so, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{grouped.Count()}개의 WeaponSO 생성 완료!");
    }
}
#endif
