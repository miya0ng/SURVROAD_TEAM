using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전기 트랩 감속을 차량 이동체에 적용하는 토큰.
/// 참조 카운팅으로 여러 트랩 중첩 지원.
/// </summary>
public class TrapSlowToken : MonoBehaviour
{
    private readonly List<float> _stacks = new();
    private IExternalSpeedScale _vehicle;
    private float _currentScale = 1f;
    
    void Awake()
    {
        _vehicle = GetComponent<IExternalSpeedScale>();
        if (_vehicle == null)
        {
            Debug.LogError($"[TrapSlowToken] {gameObject.name}에 IExternalSpeedScale이 없어 감속 불가!");
            enabled = false; // 작동 중지
        }
    }
    
    public void AddRef(float mult)
    {
        if (mult < 0.05f || mult > 1f)
        {
            Debug.LogWarning($"[TrapSlowToken] 비정상 감속값: {mult}, 클램핑됨");
        }
        _stacks.Add(Mathf.Clamp(mult, 0.05f, 1f));
        Apply();
    }
    
    public void RemoveRef(float mult)
    {
        _stacks.Remove(mult);
        Apply();
        
        // 스택이 비면 토큰 제거
        if (_stacks.Count == 0)
        {
            Destroy(this);
        }
    }
    
    public void RemoveAll()
    {
        _stacks.Clear();
        Apply();
        Destroy(this);
    }
    
    void Apply()
    {
        // 모든 스택을 곱셈 (0.5 * 0.5 = 0.25 = 75% 감속)
        float m = 1f;
        foreach (float stack in _stacks)
        {
            m *= stack;
        }
        
        m = Mathf.Clamp(m, 0.05f, 1f); // 최소 5% 속도는 보장
        _currentScale = m;
        
        if (_vehicle != null)
        {
            _vehicle.SetExternalSpeedScale(m);
        }
    }
    
    void OnDestroy()
    {
        // 안전장치: 제거 시 반드시 원래 속도로 복구
        _vehicle?.SetExternalSpeedScale(1f);
    }
    
    // 디버그용
    void OnGUI()
    {
        if (!Application.isPlaying || _stacks.Count == 0) return;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPos.z > 0)
        {
            GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y, 200, 30),
                $"Slow: {(1f - _currentScale) * 100:F0}% ({_stacks.Count} stacks)");
        }
    }
}