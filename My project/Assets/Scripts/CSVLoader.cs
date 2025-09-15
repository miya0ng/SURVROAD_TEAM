using UnityEngine;

public class CsvLoader : MonoBehaviour
{
    [SerializeField] private TextAsset csvFile;
    private WeaponData weaponData;
    void Start()
    {
        string[] lines = csvFile.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) // 0번째는 헤더니까 1부터
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] values = lines[i].Split(',');

            string name = values[0];
            int Type;
            int Target;
            int Level;

            float Damage;
            int ShotCount;
            float AttackSpeed;
            float AttackRange;
            float Dps;
            float BulletSpeed;
            float BulletLifeTime;
            float Duration;
            bool Piercing;

            string Info;
            string PrefabName;
            int PrefabIndex;
            string BulletPrefab;

            name = values[0];

            int.TryParse(values[1], out Type);
            int.TryParse(values[2], out Target);
            int.TryParse(values[3], out Level);
            float.TryParse(values[4], out Damage);
            int.TryParse(values[4], out ShotCount);
            float.TryParse(values[5], out AttackSpeed);
            float.TryParse(values[6], out AttackRange);
            float.TryParse(values[7], out Dps);
            float.TryParse(values[8], out BulletSpeed);
            float.TryParse(values[9], out BulletLifeTime);
            float.TryParse(values[10], out Duration);
            bool.TryParse(values[11], out Piercing);
            Info = values[12];
            PrefabName = values[13];
        }
    }
}