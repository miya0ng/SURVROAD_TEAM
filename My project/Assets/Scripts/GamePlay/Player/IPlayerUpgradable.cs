
/// <summary>
/// 플레이어 쪽에서 이 인터페이스를 구현하면, 업그레이드 누적치를 즉시 반영해줄 수 있음.
/// </summary>
public interface IPlayerUpgradable
{
    void ApplyMultipliers(float durabilityMul, float maxSpeedMul, float accelerationMul);
}