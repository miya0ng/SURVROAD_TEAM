using UnityEngine;

public class IntervalFire : MonoBehaviour, IFireStrategy
{
    private float t;

    public bool ShouldFire(float dt, WeaponLevelData level)
    {
        t += dt;
        if (t >= Mathf.Max(0.01f, level.AttackSpeed))
        {
            t = 0f;
            return true;
        }
        return false;
    }

    public void Reset() => t = 0f;
}