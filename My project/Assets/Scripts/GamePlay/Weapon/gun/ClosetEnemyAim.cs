using UnityEngine;

public class ClosestEnemyAim : MonoBehaviour, IAimStrategy
{
    private EnemyManager enemyManager;

    void Awake() => enemyManager = GameObject.FindWithTag("EnemySpawner").GetComponent<EnemyManager>();

    public Transform GetAimTarget(Transform self, TeamId teamId)
    {
        var enemies = enemyManager.GetEnemies();
        float closest = float.MaxValue; LivingEntity best = null;
        foreach (var e in enemies)
        {
            if (!e.gameObject.activeInHierarchy || e.teamId == teamId) continue;
            float d = Vector3.SqrMagnitude(e.transform.position - self.position);
            if (d < closest) { closest = d; best = e; }
        }
        return best ? best.transform : null;
    }

    public Quaternion GetRotationTowards(Transform self, Transform target)
    {
        Vector3 dir = target.position - self.position;
        dir.y = 0;
        return dir.sqrMagnitude < 0.001f ? self.rotation : Quaternion.LookRotation(dir, Vector3.up);
    }
}