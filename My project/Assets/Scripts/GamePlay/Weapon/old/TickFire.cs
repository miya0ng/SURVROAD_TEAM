using UnityEngine;

public class TickFire : MonoBehaviour, IFireStrategy
{
    [SerializeField] private float tick = 0.2f;
    private float t;

    public bool ShouldFire(float dt, WeaponLevelData level)
    {
        t += dt;
        if (t >= tick)
        {
            t = 0f;
            return true;
        }
        return false;
    }

    public void Reset() => t = 0f;
}