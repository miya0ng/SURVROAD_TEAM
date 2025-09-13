using System.Collections.Generic;
using UnityEngine;
using Enemy = EnemyBehaviour;
public class EnemyManager : MonoBehaviour
{
    private readonly List<Enemy> enemies = new List<Enemy>();

    public void Register(Enemy e) => enemies.Add(e);
    public void Unregister(Enemy e) => enemies.Remove(e);

    public int GetAliveEnemyCount()
    {
        return enemies.Count;
    }
}