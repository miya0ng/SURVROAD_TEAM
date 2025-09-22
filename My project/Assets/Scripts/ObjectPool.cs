using System.Collections.Generic;
using UnityEngine;

/// <summary>아주 심플한 프리팹 단위 풀</summary>
public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    private readonly Queue<GameObject> q = new();

    private static readonly Dictionary<GameObject, ObjectPool> pools = new();

    public static ObjectPool GetOrCreate(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool) || pool == null)
        {
            var go = new GameObject($"Pool_{prefab.name}");
            pool = go.AddComponent<ObjectPool>();
            pool.prefab = prefab;
            pools[prefab] = pool;
        }
        return pool;
    }

    public GameObject Pop(Vector3 pos, Quaternion rot)
    {
        GameObject obj = q.Count > 0 ? q.Dequeue() : Instantiate(prefab);
        obj.transform.SetPositionAndRotation(pos, rot);
        return obj;
    }

    public void Push(GameObject obj)
    {
        obj.SetActive(false);
        q.Enqueue(obj);
    }
}