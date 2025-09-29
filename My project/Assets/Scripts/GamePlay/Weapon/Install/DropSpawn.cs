// Assets/Scripts/Weapons/DropSpawn.cs
using UnityEngine;

public class DropSpawn : MonoBehaviour, IProjectileSpawn
{

    [Header("투척무기")]
    [SerializeField] private GameObject trapPrefabOverride;


    [Header("Drop Point (없으면 Owner 기준)")]
    [SerializeField] private Transform dropPoint;

    [Header("Ground Snap")]
    [SerializeField] private LayerMask groundMask;   // Terrain, Road, StaticProps 등 포함
    [SerializeField] private float castHeight = 5f;
    [SerializeField] private float maxCastDown = 200f;
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private bool alignToGroundNormal = true; // 지면 노멀로 회전

    [Header("Placement Rules (선택)")]
    [SerializeField] private float minPlaceGap = 0f;
    [SerializeField] private float overlapCheckRadius = 0f;
    [SerializeField] private LayerMask overlapBlockMask = 0;

    [Header("Pooling")]
    [SerializeField] private bool usePool = true;
    [SerializeField] private Transform trapsRoot;     // 씬 정리용 부모(선택)

    private Vector3 _lastPlacePos = new Vector3(float.PositiveInfinity, 0f, 0f);

    public void Spawn(WeaponContext ctx)
    {
        Debug.Log($"[DropSpawn] placed '{gameObject.name}' at {transform.position} parent={(trapsRoot ? trapsRoot.name : "WorldRoot")}");

        if (ctx.Level == null || !ctx.Level.prefab)
        {
            Debug.LogWarning("[DropSpawn] Level or prefab is null");
            return;
        }

        // 1) 스폰 원점 결정
        Transform src = dropPoint ? dropPoint : ctx.Owner.transform;
        Vector3 origin = src.position + Vector3.up * castHeight;

        // 2) 지면 스냅
        Vector3 pos = src.position;
        Quaternion rot = Quaternion.identity;

        if (Physics.Raycast(origin, Vector3.down, out var hit, castHeight + maxCastDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point + hit.normal * yOffset;
            rot = alignToGroundNormal
                ? Quaternion.FromToRotation(Vector3.up, hit.normal)
                : Quaternion.identity;
        }
        else
        {
            // 안전: 그래도 yOffset만 살짝 올려서 묻히지 않게
            pos = src.position + Vector3.up * yOffset;
            rot = Quaternion.identity;
            Debug.Log("[DropSpawn] Ground raycast failed — using fallback pos.");
        }

        // 3) 배치 규칙
        if (minPlaceGap > 0f && !_floatIsInf(_lastPlacePos.x))
        {
            if (Vector3.Distance(_lastPlacePos, pos) < minPlaceGap)
            {
                // 너무 가까워 스킵
                // Debug.Log($"[DropSpawn] Min gap({minPlaceGap}) blocked.");
                return;
            }
        }
        if (overlapCheckRadius > 0f && overlapBlockMask != 0)
        {
            if (Physics.CheckSphere(pos + Vector3.up * 0.05f, overlapCheckRadius, overlapBlockMask, QueryTriggerInteraction.Ignore))
            {
                // 겹침 차단
                // Debug.Log("[DropSpawn] Overlap blocked.");
                return;
            }
        }

        // 4) 생성 (★프리팹이 비활성 저장이어도 강제로 활성화)
        GameObject go;
        if (usePool)
        {
            var pool = ObjectPool.GetOrCreate(ctx.Level.prefab);
            go = pool.Pop(pos, rot);
            if (trapsRoot) go.transform.SetParent(trapsRoot, true);
        }
        else
        {
            go = Instantiate(ctx.Level.prefab, pos, rot);
            if (!go.activeSelf) go.SetActive(true);
            if (trapsRoot) go.transform.SetParent(trapsRoot, true);
        }

        // 5) 타입별 초기화
        bool recognized = false;
        if (go.TryGetComponent<TrapElectric>(out var electric))
        {
            electric.Init(ctx.Owner, ctx.TeamId, ctx.Level);
            recognized = true;
        }
        if (go.TryGetComponent<TrapMine>(out var mine))
        {
            mine.Init(ctx.Owner, ctx.TeamId);
            // Mine은 OnEnable에서 다시 스냅하므로 groundMask가 꼭 맞아야 함.
            recognized = true;
        }
        if (!recognized)
        {
            Debug.LogWarning("[DropSpawn] Spawned prefab has no known trap component (TrapElectric/TrapMine).");
        }

        // 6) 보이기 보조 — 렌더러가 꺼져 있으면 켜준다(에셋 실수 대비)
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend != null) rend.enabled = true;

        _lastPlacePos = pos;
        if (ctx.FireFx) ctx.FireFx.Play();

#if UNITY_EDITOR
        Debug.DrawRay(pos, Vector3.up * 0.4f, Color.yellow, 1.5f);
#endif
    }

    private static bool _floatIsInf(float v) => float.IsNaN(v) || float.IsInfinity(v);
}
