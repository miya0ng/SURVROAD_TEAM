using UnityEngine;

/// <summary>
/// 하나로 쓰는 발사 타이머 전략:
/// - FromLevel : level.AttackSpeed 사용 (Interval/Cooldown 대체)
/// - FixedTick : 고정 간격 사용 (TickFire 대체)
/// - WithJitter : 간격에 ±랜덤 지터 추가(선택)
/// - Burst      : 1회 발사 시 N발 연사(선택) → ShouldFire를 N프레임 true로 반환
/// </summary>
public class IntervalFire : MonoBehaviour, IFireStrategy
{
    public enum Mode { FromLevel, FixedTick }

    [Header("Mode")]
    [SerializeField] private Mode mode = Mode.FromLevel;

    [Header("FromLevel")]
    [Tooltip("최소 간격(0이면 무제한). IntervalFire와 동일 역할")]
    [SerializeField] private float minInterval = 0.01f;
    [Tooltip("레벨의 AttackSpeed에 곱하는 스케일(전역 밸런싱용, 1=기본)")]
    [SerializeField] private float levelScale = 1f;

    [Header("FixedTick")]
    [SerializeField] private float fixedTick = 0.2f; // TickFire 대체

    [Header("Jitter (선택)")]
    [Tooltip("간격에 ±비율 랜덤 지터 적용 (예: 0.1 → ±10%)")]
    [Range(0f, 0.9f)]
    [SerializeField] private float jitterRatio = 0f;

    [Header("Burst (선택)")]
    [SerializeField] private bool useBurst = false;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstGap = 0.1f; // 연사 간격(초)

    private float t;              // 메인 타이머
    private float intervalNow;    // 현재 타이밍 간격(지터 반영)
    private int burstLeft;        // 남은 연사 수
    private float burstTimer;     // 연사 간격 타이머

    public bool ShouldFire(float dt, WeaponLevelData level)
    {
        // 연사 중이면 burstGap 간격으로 true 반환
        if (useBurst && burstLeft > 0)
        {
            burstTimer += dt;
            if (burstTimer >= burstGap)
            {
                burstTimer = 0f;
                burstLeft--;
                return true;
            }
            return false;
        }

        t += dt;
        float baseInterval = GetBaseInterval(level);
        if (intervalNow <= 0f) intervalNow = ApplyJitter(baseInterval);

        if (t >= intervalNow)
        {
            t -= intervalNow;                // 누적 초과분 유지
            intervalNow = ApplyJitter(baseInterval);

            if (useBurst && burstCount > 1)
            {
                // 첫 발은 지금 반환, 이후 (burstCount-1)발은 burst 모드로 분배
                burstLeft = burstCount - 1;
                burstTimer = 0f;
            }
            return true;
        }
        return false;
    }

    public void Reset()
    {
        t = 0f;
        burstLeft = 0;
        burstTimer = 0f;
        intervalNow = 0f;
    }

    private float GetBaseInterval(WeaponLevelData level)
    {
        switch (mode)
        {
            case Mode.FromLevel:
                float atk = (level != null ? level.AttackSpeed : 0.2f) * Mathf.Max(0.01f, levelScale);
                return Mathf.Max(minInterval, atk);
            case Mode.FixedTick:
            default:
                return Mathf.Max(0.01f, fixedTick);
        }
    }

    private float ApplyJitter(float baseInterval)
    {
        if (jitterRatio <= 0f) return baseInterval;
        float delta = baseInterval * jitterRatio;
        return Random.Range(baseInterval - delta, baseInterval + delta);
    }
}
