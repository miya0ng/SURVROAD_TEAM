using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerBehaviour))]
public class PlayerStatusEffects : MonoBehaviour
{
    public event Action<bool> OnTurboActiveChanged;
    public event Action<float, float, bool> OnTurboCooldownChanged;

    public bool IsInvulnerable => Time.time < _invulnUntil;
    public bool IsTurboActive() => isTurboActive;
    public float AttackSpeedMul { get; private set; } = 1f;
    
    private float _invulnUntil;

    // ==== 기본 스펙 ====
    [Header("Turbo (Base Spec)")]
    [SerializeField] private bool turboUnlocked = true;
    [SerializeField] private float turboDuration = 2f;
    [SerializeField] private float turboCooldown = 10f;

    // ==== 임시 스펙(아이템 효과) ====
    private float specUntil;  // ✅ 오타 수정
    private float overrideDuration;
    private float overrideCooldown;
    public bool IsInSpecWindow => Time.time < specUntil;

    // ==== 쿨다운 상태 ====
    private float turboCooldownEnd;
    private float turboCooldownStart;
    private float activeCooldown;
    private bool isTurboActive;
    
    private Coroutine turboCoroutine;
    
    // ✅ Update 최적화용
    private float lastEventValue = -1f;
    private bool lastEventReady = false;

    private void Start()
    {
        // ✅ 초기 쿨다운 상태 설정
        UnlockTurbo(turboDuration, turboCooldown);
        
        // ✅ 명시적으로 비활성 상태임을 알림
        Debug.Log("[PlayerStatusEffects] 초기화 완료 - 터보 비활성 상태");
    }

    void Update()
    {
        if (!turboUnlocked) return;
        
        // ✅ 값이 변했을 때만 이벤트 발생
        float v, m;
        GetTurboProgress(out v, out m);
        bool ready = IsTurboReady();
        
        // 0.01초(약 1 프레임) 단위로만 업데이트
        if (Mathf.Abs(v - lastEventValue) > 0.01f || ready != lastEventReady)
        {
            lastEventValue = v;
            lastEventReady = ready;
            OnTurboCooldownChanged?.Invoke(v, m, ready);
        }
    }

    // === 버프/효과 관련 ===
    public void ApplyInvulnerability(float seconds)
    {
        _invulnUntil = Mathf.Max(_invulnUntil, Time.time + seconds);
        Debug.Log($"[Invuln] {seconds}초 무적 적용");
    }

    public void ApplyAttackSpeedBuff(float mul, float seconds)
    {
        StartCoroutine(CoAttackSpeedBuff(mul, seconds));
    }

    IEnumerator CoAttackSpeedBuff(float mul, float seconds)
    {
        AttackSpeedMul *= mul;
        Debug.Log($"[AttackSpeed] {mul}배 버프 시작 (현재: {AttackSpeedMul})");
        
        yield return new WaitForSeconds(seconds);
        
        AttackSpeedMul /= mul;
        Debug.Log($"[AttackSpeed] 버프 종료 (현재: {AttackSpeedMul})");
    }

    // === 임시 스펙 부여(아이템 효과) ===
    public void GrantTurboSpecWindow(float windowSec, float newDuration = 3f, float newCooldown = 0.2f)
    {
        // 스펙 윈도우 연장
        specUntil = Mathf.Max(specUntil, Time.time + windowSec);
        overrideDuration = newDuration;
        overrideCooldown = newCooldown;

        // 현재 쿨다운 중이면 새로운 쿨타임으로 단축 가능
        if (Time.time < turboCooldownEnd)
        {
            float newEnd = turboCooldownStart + EffectiveCooldown;
            if (newEnd < turboCooldownEnd)
            {
                turboCooldownEnd = newEnd;
                Debug.Log($"[TurboSpec] 쿨다운 {turboCooldownEnd - Time.time:F2}초로 단축");
            }
        }

        RaiseTurboEvent();
    }

    public void UnlockTurbo(float duration, float cooldown)
    {
        turboUnlocked = true;
        if (duration > 0f) turboDuration = duration;
        if (cooldown > 0f) turboCooldown = cooldown;

        // ✅ 초기 상태: 쿨다운 완료 (바로 사용 가능)
        turboCooldownStart = turboCooldownEnd = Time.time;
        activeCooldown = 0f;
        
        Debug.Log($"[Turbo] 언락: {turboDuration}초 지속, {turboCooldown}초 쿨다운");
        RaiseTurboEvent();
    }

    private float EffectiveDuration => IsInSpecWindow ? (overrideDuration > 0f ? overrideDuration : turboDuration) : turboDuration;
    private float EffectiveCooldown => IsInSpecWindow ? (overrideCooldown > 0f ? overrideCooldown : turboCooldown) : turboCooldown;

    public bool IsTurboReady()
    {
        if (!turboUnlocked) return false;
        if (isTurboActive) return false;
        return Time.time >= turboCooldownEnd;
    }

    /// <summary>터보 사용 시도 (UI 버튼 등에서 호출)</summary>
    public bool TryUseTurbo()
    {
        if (isTurboActive)
        {
            Debug.LogWarning("[Turbo] 이미 활성화 중!");
            return false;
        }

        if (!IsTurboReady())
        {
            float remaining = turboCooldownEnd - Time.time;
            Debug.LogWarning($"[Turbo] 아직 준비 안됨! ({remaining:F1}초 남음)");
            return false;
        }

        Debug.Log("[Turbo] 사용 시작!");

        // 쿨다운 설정
        activeCooldown = Mathf.Max(0.0001f, EffectiveCooldown);
        turboCooldownStart = Time.time;
        turboCooldownEnd = Time.time + activeCooldown;

        // 코루틴 시작
        if (turboCoroutine != null)
        {
            StopCoroutine(turboCoroutine);
        }
        turboCoroutine = StartCoroutine(CoTurbo());

        return true;
    }

    IEnumerator CoTurbo()
    {
        isTurboActive = true;
        Debug.Log("[Turbo] ✅ 활성화 이벤트 발동");
        OnTurboActiveChanged?.Invoke(true);  // ⭐ CarController에서 이걸 받습니다!
        
        RaiseTurboEvent();

        float duration = EffectiveDuration;
        Debug.Log($"[Turbo] {duration}초 대기...");
        yield return new WaitForSeconds(duration);

        isTurboActive = false;
        Debug.Log("[Turbo] ❌ 비활성화 이벤트 발동");
        OnTurboActiveChanged?.Invoke(false);  // ⭐ 여기서 끕니다!
        
        RaiseTurboEvent();
        
        turboCoroutine = null;
    }

    /// <summary>UI 슬라이더용 진행도</summary>
    public void GetTurboProgress(out float value, out float max)
    {
        if (!turboUnlocked)
        {
            value = 0f;
            max = 1f;
            return;
        }

        if (IsTurboReady())
        {
            max = EffectiveCooldown;
            value = max;
            return;
        }

        max = (activeCooldown > 0f) ? activeCooldown : EffectiveCooldown;
        value = Mathf.Clamp(Time.time - turboCooldownStart, 0f, max);
    }

    private void RaiseTurboEvent()
    {
        if (OnTurboCooldownChanged == null) return;

        float v, m;
        GetTurboProgress(out v, out m);
        bool ready = IsTurboReady();
        OnTurboCooldownChanged.Invoke(v, m, ready);
    }
}