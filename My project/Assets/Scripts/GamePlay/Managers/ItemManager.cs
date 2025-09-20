
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private List<ItemData> enemyDropItems = new List<ItemData>();
    private List<ItemData> objectDropItems = new List<ItemData>();
    private Dictionary<int, GameObject> itemPrefabs = new Dictionary<int, GameObject>();

    void Awake()
    {
        // 테이블에서 그룹별 아이템 분류
        var table = DataTableManger.Get<ItemDataTable>(ItemDataTable.ItemTableId);
        enemyDropItems = new List<ItemData>(table.GetItemsByDropPoint(1));
        objectDropItems = new List<ItemData>(table.GetItemsByDropPoint(2));

        foreach (var item in table.GetItemsByDropPoint(1))
            LoadPrefab(item);

        foreach (var item in table.GetItemsByDropPoint(2))
            LoadPrefab(item);
    }

    private void LoadPrefab(ItemData data)
    {
        if (string.IsNullOrEmpty(data.PrefabName)) return;

        if (!itemPrefabs.ContainsKey(data.ID))
        {
            var prefab = Resources.Load<GameObject>($"Items/{data.PrefabName}");
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {data.PrefabName}");
                return;
            }
            itemPrefabs[data.ID] = prefab;
        }
    }

    public void DropFromEnemy(Vector3 pos)
    {
        // 확정 드랍 (일반 부품)
        var common = enemyDropItems.Find(i => i.ID == 31001);
        if (common != null)
            SpawnItem(common, pos);

        // 특수 부품 (확률)
        var special = enemyDropItems.Find(i => i.ID == 31002);
        if (special != null && Random.value < special.DropRate)
            SpawnItem(special, pos);
    }

    public void DropFromObject(Vector3 pos)
    {
        float rand = Random.value;
        float cumulative = 0f;

        foreach (var item in objectDropItems)
        {
            cumulative += item.DropRate;
            if (rand <= cumulative)
            {
                SpawnItem(item, pos);
                break;
            }
        }
    }

    private void SpawnItem(ItemData data, Vector3 pos)
    {
        if (!itemPrefabs.TryGetValue(data.ID, out var prefab))
        {
            Debug.LogWarning($"Prefab not cached for item: {data.Name} ({data.PrefabName})");
            return;
        }

        var obj = Instantiate(prefab, pos, Quaternion.identity);
        var itemBase = obj.GetComponent<ItemBase>();
        if (itemBase != null)
            itemBase.itemData = data; // 데이터 주입
    }
}
