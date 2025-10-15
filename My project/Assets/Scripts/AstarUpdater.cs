// AstarUpdater.cs
using Pathfinding;
using UnityEngine;

public static class AstarUpdater
{
    // 여러 타일을 한 번에 갱신하고 싶으면 Encapsulate로 합쳐서 한 번만 호출
    public static void RefreshArea(Bounds b)
    {
        var guo = new GraphUpdateObject(b)
        {
            updatePhysics = true,
            // resetPenaltyOnPhysics = true, // 필요시
        };
        AstarPath.active.UpdateGraphs(guo);
    }
}