using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private float range = 100f;
    [SerializeField] private int spawnCount = 30;
    private Vector3 center = Vector3.zero;

    public WeaponLibrary WeaponLibrary;

    private void Start()
    {
        CreateItem();
    }

    private bool SpawnPosition(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            if (NavMesh.SamplePosition(randomPoint, out var hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    public void CreateItem()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            if (!SpawnPosition(center, range, out var result))
                continue;

            var pos = result;
            pos.y += 0.5f;

            int randomIndex = Random.Range(0, WeaponLibrary.weapons.Count);
            var so = WeaponLibrary.weapons[randomIndex];
            if (so == null || so.prefab == null)
            {
                Debug.LogWarning("WeaponSO or prefab is null");
                continue;
            }

            var rot = Quaternion.Euler(0f, Random.Range(0f, 120f), 0f);
            var instance = Instantiate(so.prefab, pos, rot);
            instance.transform.localScale = new Vector3(3f, 3f, 3f);

            var weapon = instance.GetComponent<Weapon>();

            weapon.weaponSO = so;
            weapon.curLevel = 1;
        }
    }
}
