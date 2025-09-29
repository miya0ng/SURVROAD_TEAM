// 파일 상단 using들 그대로 유지
using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerBehaviour))]
public class PlayerStatusEffects : MonoBehaviour
{
    public event System.Action<bool> OnTurboActiveChanged; // true=활성 시작, false=활성 종료
    /// <summary>진행도 이벤트: (value, max, isReady)</summary>
    public event Action<float, float, bool> OnTurboCooldownChanged;

    public bool IsInvulnerable => Time.time < _invulnUntil;
    public bool IsTurboActive() => isTurboActive;
    public float AttackSpeedMul { get; private set; } = 1f;
    public bool TurboUnlimited => Time.time < _turboUntil;

    float _invulnUntil;
    float _turboUntil;

    // ==== 터보 Unlock/쿨다운 진행 ====
    [Header("Turbo")]
    [SerializeField] private bool turboUnlocked = false;
    [SerializeField] private float turboDuration = 5f;   // 효과 지속
    [SerializeField] private float turboCooldown = 1f;   // 쿨다운 길이

    float _turboCdEnd;    // 쿨다운 종료 시각(Time.time)
    float _turboCdStart;  // 쿨다운 시작 시각
    bool isTurboActive;   // 활성 중 여부 내부 상태

    void Update()
    {
        // 쿨다운 진행 이벤트 갱신 (UI 슬라이더용)
        if (turboUnlocked)
        {
            RaiseTurboEvent();
        }
    }

    // === 외부 시스템에서 참조할 공개 메서드/프로퍼티 ===
    public void ApplyInvulnerability(float seconds) =>
        _invulnUntil = Mathf.Max(_invulnUntil, Time.time + seconds);

    public void ApplyAttackSpeedBuff(float mul, float seconds)
    {
        StartCoroutine(CoAttackSpeedBuff(mul, seconds));
    }

    public void ApplyTurboUnlimited(float seconds) =>
        _turboUntil = Mathf.Max(_turboUntil, Time.time + seconds);

    IEnumerator CoAttackSpeedBuff(float mul, float seconds)
    {
        AttackSpeedMul *= mul;
        yield return new WaitForSeconds(seconds);
        AttackSpeedMul /= mul;
    }

    /// <summary>아이템으로 터보를 언락(버튼 활성 조건 충족)</summary>
    public void UnlockTurbo(float duration, float cooldown)
    {
        turboUnlocked = true;
        if (duration > 0f) turboDuration = duration;
        if (cooldown > 0f) turboCooldown = cooldown;

        // 언락 즉시 '준비완료' 상태로 세팅
        _turboCdStart = _turboCdEnd = Time.time;

        // UI 즉시 반영
        RaiseTurboEvent();
    }

    public bool IsTurboReady()
    {
        if (!turboUnlocked) return false;
        return Time.time >= _turboCdEnd;
    }

    /// <summary>버튼 눌렀을 때 실제 사용</summary>
    public bool TryUseTurbo()
    {
        if (!IsTurboReady()) return false;

        // 효과 적용 시간(무적/공격속도 증감 등은 게임 규칙에 맞춰 추가 가능)
        // 여기서는 "무제한 부스트 판정" 타이머도 함께 세팅
        _turboUntil = Time.time + turboDuration;

        // 쿨다운 시작 즉시 기록
        _turboCdStart = Time.time;
        _turboCdEnd = Time.time + turboCooldown;

        // UI 즉시 반영(슬라이더 0으로 떨어짐)
        RaiseTurboEvent();

        // 지속시간 코루틴으로 활성 시작/종료 이벤트 브로드캐스트
        StartCoroutine(CoTurbo());
        return true;
    }

    IEnumerator CoTurbo()
    {
        isTurboActive = true;
        OnTurboActiveChanged?.Invoke(true);

        // 실제 지속시간(타임스케일 정책은 프로젝트 규칙에 맞춰 WaitForSeconds/Unscaled로 조정)
        yield return new WaitForSeconds(turboDuration);

        isTurboActive = false;
        OnTurboActiveChanged?.Invoke(false);
        // 이후 쿨다운은 Update에서 RaiseTurboEvent로 계속 갱신됨
    }

    /// <summary>UI 슬라이더에 넣을 값 계산: (value=경과, max=총쿨)</summary>
    public void GetTurboProgress(out float value, out float max)
    {
        if (!turboUnlocked)
        {
            value = 0f; max = 1f; return;
        }

        max = Mathf.Max(0.0001f, turboCooldown);
        if (IsTurboReady())
        {
            value = max;   // 준비완료 → 슬라이더 가득 참
        }
        else
        {
            float elapsed = Mathf.Clamp(Time.time - _turboCdStart, 0f, turboCooldown);
            value = elapsed;
        }
    }

    void RaiseTurboEvent()
    {
        if (OnTurboCooldownChanged == null) return;

        float v, m;
        GetTurboProgress(out v, out m);
        bool ready = v >= m - 0.0001f;
        OnTurboCooldownChanged.Invoke(v, m, ready);
    }
}
