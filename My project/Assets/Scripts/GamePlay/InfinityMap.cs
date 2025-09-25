using UnityEngine;
using System.Collections.Generic;

public class InfiniteMap : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject tilePrefab;

    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Queue<GameObject> tilePool = new();

    private Vector2Int currentCenter;
    private const float TILE_SIZE = 300f;
    private const int RANGE = 1;

    // 바닥이 y=0이면 0, 네 맵에 맞춰 수정
    private float groundY = 0f;
    private float tileHeight = 50f; // 타일 높이(충분히 크게)

    void Start()
    {
        for (int i = 0; i < 12; i++)
        {
            var obj = Instantiate(tilePrefab);
            obj.SetActive(false);
            tilePool.Enqueue(obj);
        }

        // (선택) 런타임에 Graph 크기/Center를 초기화하고 싶으면 여기서 한번 세팅
        BootstrapGridGraph();

        UpdateTiles(force: true);
    }

    void Update()
    {
        Vector2Int playerTile = GetTileCoord(player.position);
        if (playerTile != currentCenter)
        {
            currentCenter = playerTile;
            UpdateTiles(force: false);
        }
    }

    Vector2Int GetTileCoord(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pos.x / TILE_SIZE),
            Mathf.FloorToInt(pos.z / TILE_SIZE)
        );
    }

    void UpdateTiles(bool force)
    {
        Vector2Int center = GetTileCoord(player.position);
        HashSet<Vector2Int> needed = new();

        // 이번 프레임에 새로 켠 타일들의 총 바운즈(한 번만 갱신하기 위함)
        bool anyAdded = false;
        Bounds total = default;

        for (int x = -RANGE; x <= RANGE; x++)
        {
            for (int z = -RANGE; z <= RANGE; z++)
            {
                Vector2Int coord = center + new Vector2Int(x, z);
                needed.Add(coord);

                if (!activeTiles.ContainsKey(coord))
                {
                    GameObject tile = GetTileFromPool();
                    // 타일 배치
                    Vector3 pos = new Vector3(
                        coord.x * TILE_SIZE,
                        groundY,                  // 바닥 높이에 맞춰
                        coord.y * TILE_SIZE
                    );
                    tile.transform.position = pos;
                    tile.SetActive(true);
                    activeTiles.Add(coord, tile);

                    // 바운즈 누적(가로세로 TILE_SIZE, 세로 tileHeight)
                    var b = new Bounds(
                        pos + new Vector3(TILE_SIZE * 0.5f, tileHeight * 0.5f, TILE_SIZE * 0.5f),
                        new Vector3(TILE_SIZE, tileHeight, TILE_SIZE)
                    );
                    if (!anyAdded) { total = b; anyAdded = true; }
                    else total.Encapsulate(b);
                }
            }
        }

        // 범위 밖 타일 회수
        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeTiles)
        {
            if (!needed.Contains(kvp.Key))
            {
                ReturnTileToPool(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var r in toRemove) activeTiles.Remove(r);

        // 새로 켠 영역만 그래프 갱신
        if (anyAdded)
        {
            // 여유 패딩을 조금 줘서 경계 노드까지 재샘플
            float pad = 2f;
            total.Expand(new Vector3(pad, pad, pad));
            AstarUpdater.RefreshArea(total);
        }
    }

    GameObject GetTileFromPool()
    {
        if (tilePool.Count > 0)
            return tilePool.Dequeue();
        return Instantiate(tilePrefab);
    }

    void ReturnTileToPool(GameObject tile)
    {
        tile.SetActive(false);
        // (선택) 타일 제거 영역도 갱신하고 싶으면 여기서 Bounds 만들고 RefreshArea 호출
        tilePool.Enqueue(tile);
    }

    void BootstrapGridGraph()
    {
#if ASTARPATHFINDINGPROJECT
        var astar = Pathfinding.AstarPath.active;
        if (astar == null) return;
        var data = astar.data;
        var gg = data.gridGraph;
        if (gg == null) return;

        float nodeSize = 2f; // 위 추천과 동일
        int spanMeters = (int)((RANGE * 2 + 1) * TILE_SIZE);
        int margin = 32;
        int width  = Mathf.CeilToInt(spanMeters / nodeSize) + margin;
        int depth  = Mathf.CeilToInt(spanMeters / nodeSize) + margin;

        gg.center = new Vector3(player.position.x, groundY + 3f, player.position.z);
        gg.SetDimensions(width, depth, nodeSize);
        astar.Scan(); // 최초 1회

        // ProceduralGridMover가 붙어 있다면 여기서 target만 지정
        var mover = astar.GetComponent<Pathfinding.ProceduralGridMover>();
        if (mover != null)
        {
            mover.target = player;
            mover.graph = gg;
            mover.updateDistance = 16;   // 노드 기준
            mover.updateInterval = 0.1f; // 초
        }
#endif
    }
}
