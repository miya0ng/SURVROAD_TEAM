#if UNITY_EDITOR
using System.Collections.Generic;
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

        // 🔑 ID 앞 두 자리 기준 그룹화 (예: 2111~2115 → 211)
        var grouped = records.GroupBy(r => r.ID / 10);

        foreach (var group in grouped)
        {
            var first = group.First();

            // 무기 SO 생성
            var so = ScriptableObject.CreateInstance<WeaponSO>();
            so.ID = group.Key * 10;  // 그룹 대표 ID
            so.Name = first.Name.Split(new[] { "_lv" }, System.StringSplitOptions.None)[0];
            so.Type = first.Type;
            so.Kind = first.Kind;
            so.Target = first.Target;
            so.Levels = new List<WeaponLevelData>();

            foreach (var record in group.OrderBy(r => r.Level))
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

                if (!string.IsNullOrEmpty(record.PrefabName) &&
                    System.Enum.TryParse(record.PrefabName, out WeaponIndex index))
                {
                    levelData.PrefabIndex = index;
                }
                else
                {
                    Debug.LogWarning($"[Importer] WeaponIndex 변환 실패: {record.PrefabName}");
                }

                // Prefab (Assets/Prefabs/Item/EquipItem)
                string[] prefabGuids = AssetDatabase.FindAssets(record.PrefabName + " t:prefab", new[] { "Assets/Prefabs/Weapon" });
                if (prefabGuids.Length > 0)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                    levelData.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
                else
                {
                    Debug.LogWarning($"[Importer] Prefab 없음: {record.PrefabName}");
                }

                // Thumbnail (Assets/Textures/WeaponThumbNail)
                string[] spriteGuids = AssetDatabase.FindAssets(record.PrefabName + " t:sprite", new[] { "Assets/Textures/WeaponThumbNail" });
                if (spriteGuids.Length > 0)
                {
                    string spritePath = AssetDatabase.GUIDToAssetPath(spriteGuids[0]);
                    levelData.ThumbNail = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }
                else
                {
                    Debug.LogWarning($"[Importer] Thumbnail 없음: {record.PrefabName}");
                }

                // Bullet Prefab (Assets/Prefabs/Player)
                string[] bulletGuids = AssetDatabase.FindAssets("Bullet t:prefab", new[] { "Assets/Prefabs/Player" });
                if (bulletGuids.Length > 0)
                {
                    string bulletPath = AssetDatabase.GUIDToAssetPath(bulletGuids[0]);
                    levelData.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
                }
                else
                {
                    Debug.LogWarning("[Importer] 기본 Bullet 프리팹 없음 (Assets/Prefabs/Player)");
                }

                // 아직 effectPrefab 없음
                levelData.effectPrefab = null;

                so.Levels.Add(levelData);
            }

            // SO 저장
            string assetPath = $"{folderPath}/{so.Name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponSO>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerializedManagedFieldsOnly(so, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(so, assetPath);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{grouped.Count()}개의 WeaponSO 생성/업데이트 완료!");
    }
}
#endif
