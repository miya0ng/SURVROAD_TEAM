using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    // 풀에서 꺼낼 때 상태 초기화
    void OnPoppedFromPool();
    // 풀에 반환될 때 상태 정리
    void OnPushedToPool();
}

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int prewarm = 0;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private readonly Queue<GameObject> q = new();
    private static readonly Dictionary<GameObject, ObjectPool> pools = new();

    public static ObjectPool GetOrCreate(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var pool) || pool == null)
        {
            var go = new GameObject($"Pool_{prefab.name}");
            pool = go.AddComponent<ObjectPool>();
            pool.prefab = prefab;
            // 프리팹 자체를 비활성화해두면 Instantiate도 비활성 인스턴스로 나온다.
            pool.prefab.SetActive(false);
            pools[prefab] = pool;

            if (pool.dontDestroyOnLoad) DontDestroyOnLoad(go);
            if (pool.prewarm > 0) pool.Prewarm(pool.prewarm);
        }
        return pool;
    }

    private void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab, transform);
            obj.name = $"{prefab.name}_Pooled_{i}";
            q.Enqueue(obj); // 비활성 상태로 큐에 저장
        }
    }

    public GameObject Pop(Vector3 pos, Quaternion rot)
    {
        GameObject obj = q.Count > 0 ? q.Dequeue() : Instantiate(prefab, transform);

        // 1) 위치/회전 세팅 (활성화 이전)
        obj.transform.SetPositionAndRotation(pos, rot);

        // 2) 활성화
        if (!obj.activeSelf) obj.SetActive(true);

        // 3) 풀 훅
        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnPoppedFromPool();

        // 4) 물리 잔상 제거(선택)
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
        return obj;
    }

    public void Push(GameObject obj)
    {
        if (!obj || !obj.scene.IsValid()) return;

        // 1) 풀 훅
        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnPushedToPool();

        // 2) 물리 리셋(선택)
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        // 3) 비활성 + 부모 이동 + 큐로 복귀
        if (obj.activeSelf) obj.SetActive(false);
        obj.transform.SetParent(transform, false);
        q.Enqueue(obj);
    }
}
