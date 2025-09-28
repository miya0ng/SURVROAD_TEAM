using System.Collections.Generic;
using UnityEngine;

public static class EnemyQuery
{
    // activenemy 리스트로 변경해야함
    public static IEnumerable<GameObject> GetEnemiesInView(Camera cam)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            if (!e) continue;
            Vector3 vp = cam.WorldToViewportPoint(e.transform.position);
            if (vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f)
                yield return e;
        }
    }
}
