using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    // 풀에 반환될 때 상태 초기화
    void OnPushedToPool();
    // 풀에서 꺼낼 때 상태 초기화
    void OnPoppedFromPool();
}

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int prewarm = 0;       // 선택: 시작 시 미리 만들어두기
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
            // 프리팹은 비활성으로 저장해두면 Instantiate가 비활성 인스턴스를 만들고,
            // Pop에서 SetActive(true)로 활성화합니다.
            pool.prefab.SetActive(false);
            pools[prefab] = pool;

            if (pool.dontDestroyOnLoad) DontDestroyOnLoad(go);
            // 필요 시 프리워밍
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
            q.Enqueue(obj); // 비활성 상태(프리팹이 비활성이므로)로 큐에 저장
        }
    }

    public GameObject Pop(Vector3 pos, Quaternion rot)
    {
        GameObject obj = q.Count > 0 ? q.Dequeue() : Instantiate(prefab, transform);
        // 위치·회전 세팅은 활성화 이전에 먼저
        obj.transform.SetPositionAndRotation(pos, rot);

        // 활성화
        if (!obj.activeSelf) obj.SetActive(true);

        // 상태 초기화 훅
        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnPoppedFromPool();

        // 물리 잔상 제거(선택)
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
        // 중복 Push 방지: 이미 우리 자식 + 비활성 + 큐 안에 있을 확률 차단
        if (!obj || !obj.scene.IsValid()) return;

        // 상태 초기화 훅 먼저
        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnPushedToPool();

        // 물리 비활성화/정리(선택)
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            // 필요 시 isKinematic 조절 등
        }

        // 비활성 후 부모를 풀로
        if (obj.activeSelf) obj.SetActive(false);
        obj.transform.SetParent(transform, false);

        q.Enqueue(obj);
    }
}
