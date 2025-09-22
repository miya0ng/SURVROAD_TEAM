using UnityEngine;

public class CooldownFire : MonoBehaviour, IFireStrategy
{
    private float t;

    public bool ShouldFire(float dt, WeaponLevelData levelData)
    {
        t += dt;
        if (t >= levelData.AttackSpeed)
        {
            t = 0f;
            return true;
        }
        return false;
    }

    public void Reset() => t = 0f;
}