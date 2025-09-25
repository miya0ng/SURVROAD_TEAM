using Pathfinding;
using UnityEngine;

public class EnemyTargetBinder : MonoBehaviour
{
    void Awake()
    {
        var player = GameObject.FindWithTag("Player");
        var setter = GetComponent<AIDestinationSetter>();
        if (player && setter) setter.target = player.transform;
    }
}
