using UnityEngine;
using System.Collections.Generic;

public class InfiniteMap : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject tilePrefab;

    private Dictionary<Vector2Int, GameObject> activeTiles = new();
    private Queue<GameObject> tilePool = new();

    private Vector2Int currentCenter;
    private const float TILE_SIZE = 30f;
    private const int RANGE = 1; // 주변 타일 거리 → 1이면 3x3 유지

    void Start()
    {
        // 풀 생성 (9개 + 여유분)
        for (int i = 0; i < 12; i++)
        {
            var obj = Instantiate(tilePrefab);
            obj.SetActive(false);
            tilePool.Enqueue(obj);
        }

        UpdateTiles(force: true);
    }

    void Update()
    {
        Vector2Int playerTile = GetTileCoord(player.position);

        // 플레이어가 다른 타일로 넘어간 경우에만 업데이트
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

        for (int x = -RANGE; x <= RANGE; x++)
        {
            for (int z = -RANGE; z <= RANGE; z++)
            {
                Vector2Int coord = center + new Vector2Int(x, z);
                needed.Add(coord);

                if (!activeTiles.ContainsKey(coord))
                {
                    GameObject tile = GetTileFromPool();
                    tile.transform.position = new Vector3(coord.x * TILE_SIZE, 0, coord.y * TILE_SIZE);
                    tile.SetActive(true);
                    activeTiles.Add(coord, tile);
                }
            }
        }

        // 필요 없는 타일 반환
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
        tilePool.Enqueue(tile);
    }
}
