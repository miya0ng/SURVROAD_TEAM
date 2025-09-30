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
    float _invulnUntil;

    // ==== 기본 스펙 ====
    [Header("Turbo (Base Spec)")]
    [SerializeField] private bool turboUnlocked = true;   // 시작부터 사용 가능
    [SerializeField] private float turboDuration = 2f;    // 기본 지속: 2초
    [SerializeField] private float turboCooldown = 10f;   // 기본 쿨: 10초

    // ==== 임시 스펙(아이템 효과) ====
    float specUntill;           // 임시 스펙 종료 시각
    float overrideDuration;    // 임시 지속
    float overrideCooldown;    // 임시 쿨
    public bool IsInSpecWindow => Time.time < specUntill;

    // ==== 쿨다운 상태 ====
    float turboCooldownEnd;          // 쿨다운 종료 시각(Time.time)
    float turboCooldownStart;        // 쿨다운 시작 시각
    float activeCooldown;      // 이번 쿨다운에 실제 사용된 쿨 값(진행바 정규화 기준)
    bool isTurboActive;        // 현재 활성 중인지


    private void Start()
    {
        UnlockTurbo(3f, 10f); // 테스트용, 시작부터 해제
    }
    void Update()
    {
        if (turboUnlocked)
            RaiseTurboEvent(); // UI 진행도 갱신
    }

    // === 버프/효과 유틸 ===
    public void ApplyInvulnerability(float seconds) =>
        _invulnUntil = Mathf.Max(_invulnUntil, Time.time + seconds);

    public void ApplyAttackSpeedBuff(float mul, float seconds) =>
        StartCoroutine(CoAttackSpeedBuff(mul, seconds));

    IEnumerator CoAttackSpeedBuff(float mul, float seconds)
    {
        AttackSpeedMul *= mul;
        yield return new WaitForSeconds(seconds);
        AttackSpeedMul /= mul;
    }

    // === 임시 스펙 윈도우 부여(아이템 효과) ===
    // windowSec 동안 지속/쿨다운을 임시 값으로 사용
    public void GrantTurboSpecWindow(float windowSec, float newDuration = 3f, float newCooldown = 0.2f)
    {
        specUntill = Mathf.Max(specUntill, Time.time + windowSec);
        overrideDuration = newDuration;
        overrideCooldown = newCooldown;

        // 이미 쿨다운 중이면, 임시 쿨이 더 짧을 수 있으니 종료시각을 당겨줌
        if (Time.time < turboCooldownEnd)
        {
            float newEnd = turboCooldownStart + EffectiveCooldown;
            if (newEnd < turboCooldownEnd) turboCooldownEnd = newEnd;
        }

        RaiseTurboEvent(); // UI 즉시 반영
    }

    // 필요시 외부에서 기본값 재설정
    public void UnlockTurbo(float duration, float cooldown)
    {
        turboUnlocked = true;
        if (duration > 0f) turboDuration = duration;
        if (cooldown > 0f) turboCooldown = cooldown;

        // 시작부터 Ready로
        turboCooldownStart = turboCooldownEnd = Time.time;
        activeCooldown = 0f;
        RaiseTurboEvent();
    }

    // 현재 시점의 ‘적용될’ 스펙
    float EffectiveDuration => IsInSpecWindow ? (overrideDuration > 0f ? overrideDuration : turboDuration) : turboDuration;
    float EffectiveCooldown => IsInSpecWindow ? (overrideCooldown > 0f ? overrideCooldown : turboCooldown) : turboCooldown;

    public bool IsTurboReady()
    {
        if (!turboUnlocked) return false;
        return Time.time >= turboCooldownEnd;
    }

    /// <summary>버튼 눌렀을 때 실제 사용</summary>
    public bool TryUseTurbo()
    {
        if (!IsTurboReady()) return false;

        // 현재 시점의 스펙으로 쿨다운 확정(임시 스펙이면 0.2초 반영)
        activeCooldown = Mathf.Max(0.0001f, EffectiveCooldown);
        turboCooldownStart = Time.time;
        turboCooldownEnd = Time.time + activeCooldown;

        RaiseTurboEvent();

        if (!isTurboActive) StartCoroutine(CoTurbo());
        return true;
    }

    IEnumerator CoTurbo()
    {
        isTurboActive = true;
        OnTurboActiveChanged?.Invoke(true);

        // 현재 시점의 지속시간(임시 스펙이면 3초 반영)
        yield return new WaitForSeconds(EffectiveDuration);

        isTurboActive = false;
        OnTurboActiveChanged?.Invoke(false);
    }

    /// <summary>UI 슬라이더에 넣을 값 계산: (value=경과, max=총쿨)</summary>
    public void GetTurboProgress(out float value, out float max)
    {
        if (!turboUnlocked)
        {
            value = 0f; max = 1f; return;
        }

        // Ready 상태면 현재 적용 스펙 기준으로 가득 참
        if (IsTurboReady())
        {
            max = EffectiveCooldown;
            value = max;
            return;
        }

        // 쿨다운 진행 중: 이번 쿨다운에 ‘확정된’ 값을 기준으로 정규화
        max = (activeCooldown > 0f) ? activeCooldown : EffectiveCooldown;
        value = Mathf.Clamp(Time.time - turboCooldownStart, 0f, max);
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
