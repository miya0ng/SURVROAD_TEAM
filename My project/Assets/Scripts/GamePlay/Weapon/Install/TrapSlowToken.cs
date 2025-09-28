using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전기 트랩 슬로우를 차량 이동계에 곱해주는 토큰.
/// NavMeshAgent 불필요. A* 기반 차량에도 동작.
/// </summary>
public class TrapSlowToken : MonoBehaviour
{
    private readonly List<float> _stacks = new(); // 여러 트랩 중첩 대응
    private IExternalSpeedScale _vehicle;
    private float _currentScale = 1f;

    void Awake()
    {
        _vehicle = GetComponent<IExternalSpeedScale>();
        if (_vehicle == null)
            Debug.LogWarning("[TrapSlowToken] IExternalSpeedScale 구현체가 없어 슬로우가 보이지 않을 수 있음.");
    }

    public void AddRef(float mult) { _stacks.Add(Mathf.Clamp(mult, 0.05f, 1f)); Apply(); }
    public void RemoveRef(float mult) { _stacks.Remove(mult); Apply(); }
    public void RemoveAll() { _stacks.Clear(); Apply(); Destroy(this); }

    void Apply()
    {
        float m = 1f;
        for (int i = 0; i < _stacks.Count; i++) m *= _stacks[i]; // 곱연산(강하게 겹침)
        m = Mathf.Clamp(m, 0.05f, 1f);

        _currentScale = m;
        _vehicle?.SetExternalSpeedScale(m);
    }

    void OnDestroy()
    {
        // 원복
        _vehicle?.SetExternalSpeedScale(1f);
    }
}

/// <summary>
/// 차량/적 이동 컨트롤러가 구현: 외부 슬로우 배율을 곱해준다.
/// </summary>
public interface IExternalSpeedScale
{
    void SetExternalSpeedScale(float scale);
}
