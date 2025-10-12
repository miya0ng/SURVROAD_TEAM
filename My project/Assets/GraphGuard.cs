using UnityEngine;
using Pathfinding;

public class GraphGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void DumpAll()
    {
        var arr = Object.FindObjectsByType<AstarPath>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        Debug.Log($"[A* Dump] count={arr.Length}");
        foreach (var a in arr)
        {
            var t = a.transform;
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            var scn = a.gameObject.scene;
            Debug.Log($"[A*] Scene='{scn.name}'({(scn.IsValid() ? (scn.isLoaded ? "loaded" : "unloaded") : "invalid")}) Path={path} ActiveSelf={a.gameObject.activeSelf}");
        }
    }
    void Awake()
    {
        int count = Object.FindObjectsByType<AstarPath>(
                FindObjectsInactive.Include,  // 비활성 오브젝트도 포함할지
                FindObjectsSortMode.None      // 정렬 불필요
            ).Length;

        Debug.Log($"AstarPath count = {count}");

        var mine = GetComponent<AstarPath>();
        if (AstarPath.active != null && AstarPath.active != mine)
        {
            Destroy(gameObject);
            return;
        }
       // DontDestroyOnLoad(gameObject);
    }
}