using System.Collections.Generic;
using System.Drawing;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : MonoBehaviour
{
    private float range = 100f;
    private int spawnCount = 15;
    Vector3 center = Vector3.zero;
    Vector3 result;
    public WeaponLibrary WeaponLibrary;

    //GameManager gameManager;
    public void Start()
    {
        CreateItem();
    }
    public void Update()
    {

    }

    public bool SpawnPosition(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
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
        for (int i = 0; i < 15; i++)
        {
            if (SpawnPosition(center, range, out result))
            {
                var pos = result;
                pos.y += 0.5f;
                var item = WeaponLibrary.GetPrefab((PrefabIndex)(Random.Range(0, WeaponLibrary.prefabs.Count)));
                var randomRotationY = Random.Range(0, 120);
                item.transform.rotation = Quaternion.Euler(0f, randomRotationY, 0f);
                item.transform.localScale = new Vector3(3f, 3f, 3f);
                Instantiate(item, pos, item.transform.rotation);
            }
        }
    }
}