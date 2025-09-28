// StunOnRigidbody.cs
using System.Collections;
using UnityEngine;

public class StunOnRigidbody : MonoBehaviour
{
    Rigidbody rb;
    float _stunUntil;
    float _origDrag, _origAngularDrag;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            _origDrag = rb.linearDamping;
            _origAngularDrag = rb.angularDamping;
        }
    }

    public void Stun(float seconds)
    {
        _stunUntil = Mathf.Max(_stunUntil, Time.time + seconds);
        StopAllCoroutines();
        StartCoroutine(CoStun());
    }

    IEnumerator CoStun()
    {
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 1000f; // 사실상 정지
            rb.angularDamping = 1000f;
        }

        // 적 AI 스크립트 일시 비활성(옵션)
        var ai = GetComponent<MonoBehaviour>(); // EnemyCarController 등 필요 시 배열로 꺼도 됨
        // 여기선 안전하게 두고, 물리로만 정지

        while (Time.time < _stunUntil)
        {
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            yield return null;
        }

        if (rb)
        {
            rb.linearDamping = _origDrag;
            rb.angularDamping = _origAngularDrag;
        }
    }
}
