using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private float range = 100f;
    [SerializeField] private int spawnCount = 30;
    [SerializeField]
    private GameObject[] equipItemPrefabs;

    private Vector3 center = Vector3.zero;

    public Coroutine coroutine;
    private float spawnInterval = 3f;

    private void Start()
    {
        StartSpawner();
    }

    public void StartSpawner()
    {
        coroutine = StartCoroutine(CoSpawnItem());
    }

    public void StopSpawner()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);
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

    public IEnumerator CoSpawnItem()
    {
        while (true)
        {
            CreateItem();
            yield return new WaitForSeconds(spawnInterval);
        }
    }


    public void CreateItem()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            if (!SpawnPosition(center, range, out var result))
                continue;

            var pos = result;
            pos.y += 0.5f;

            int randomIndex = Random.Range(0, System.Enum.GetValues(typeof(WeaponIndex)).Length);

            // SO 가져오기
            var so = equipItemPrefabs[randomIndex].GetComponent<EquipItem>().weaponSO;
            if (so == null)
            {
                Debug.LogWarning("WeaponSO is null");
                continue;
            }

            // 아이템 프리팹 생성
            var rot = Quaternion.Euler(0f, Random.Range(0f, 120f), 0f);
            var instance = Instantiate(equipItemPrefabs[randomIndex], pos, rot);
            instance.transform.localScale = new Vector3(3f, 3f, 3f);

            // 아이템에 SO 연결
            var equipItem = instance.GetComponent<EquipItem>();
            if (equipItem != null)
            {
                equipItem.weaponSO = so;
            }
        }
    }
}