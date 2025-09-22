public interface IFireStrategy
{
    bool ShouldFire(float deltaTime, WeaponLevelData levelData);
    void Reset();
}