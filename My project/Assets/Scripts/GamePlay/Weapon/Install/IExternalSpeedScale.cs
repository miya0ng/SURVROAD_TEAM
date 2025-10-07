/// <summary>
/// 차량/적 이동 컨트롤러가 구현: 외부 속도 배율을 받아서 적용한다.
/// 전기 트랩, 아이템 등 외부 효과가 속도에 영향을 줄 때 사용.
/// </summary>
public interface IExternalSpeedScale
{
    /// <summary>
    /// 외부에서 속도 배율을 설정합니다.
    /// </summary>
    /// <param name="scale">속도 배율 (0.5 = 50% 속도, 1.0 = 정상 속도)</param>
    void SetExternalSpeedScale(float scale);
}