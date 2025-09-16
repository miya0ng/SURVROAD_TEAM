#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Recorder.OutputPath;

public class WeaponSOImporter : EditorWindow
{
    private TextAsset csvFile;

    [MenuItem("Tools/Import WeaponSO From CSV")]
    public static void ShowWindow()
    {
        GetWindow<WeaponSOImporter>("WeaponSO Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV → WeaponSO 변환", EditorStyles.boldLabel);

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

        // DataSO 폴더 없으면 생성
        if (!AssetDatabase.IsValidFolder(dataSoFolder))
        {
            AssetDatabase.CreateFolder(root, "DataSO");
        }

        // Weapons 폴더 없으면 생성
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(dataSoFolder, "Weapons");
        }
        foreach (var record in records)
        {
            var so = ScriptableObject.CreateInstance<WeaponSO>();

            so.ID = record.ID;
            so.Name = record.Name;
            so.Type = record.Type;
            so.Target = record.Target;
            so.Level = record.Level;
            so.Damage = record.Damage;
            so.ShotCount = record.ShotCount;
            so.AttackSpeed = record.AttackSpeed;
            so.AttackRange = record.AttackRange;
            so.Dps = record.Dps;
            so.BulletSpeed = record.BulletSpeed;
            so.EffectiveRange = record.EffectiveRange;
            so.ExplosionRange = record.ExplosionRange;
            so.Duration = record.Duration;
            so.Piercing = record.Piercing;
            so.Info = record.Info;

            if (!string.IsNullOrEmpty(record.PrefabName) &&
                System.Enum.TryParse(record.PrefabName, out WeaponIndex index))
            {
                so.PrefabIndex = index;
            }

            string assetPath = $"{folderPath}/{record.ID}_{record.Name}.asset";
            AssetDatabase.CreateAsset(so, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{records.Count}개의 WeaponSO 생성 완료!");
    }
}
#endif
