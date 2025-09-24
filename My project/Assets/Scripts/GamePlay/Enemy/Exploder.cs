// Assets/Scripts/Enemy/Combat/Exploder.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Exploder : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private LayerMask damageLayers;     // 데미지를 줄 레이어(플레이어/소환물 등)
    [SerializeField] private float radius = 6f;          // 폭발 반경
    [SerializeField] private bool useFalloff = true;     // 거리 비례 감쇠
    [SerializeField]
    private AnimationCurve falloff =    // 0~1 (가까울수록 1)
        AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Physics")]
    [SerializeField] private float explosionForce = 12f; // AddExplosionForce
    [SerializeField] private float upwardModifier = 0.5f;

    [Header("FX")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.9f;

    [Header("Options")]
    [SerializeField] private bool killSelfAfterExplode = true; // 자기 제거(= 풀 반환 트리거)
    [SerializeField] private float delayBeforeExplode = 0f;    // 점화 후 지연
    [SerializeField] private bool oneShot = true;              // 중복 방지

    private bool triggered;
    private Transform _attacker;  // 데미지 가해자(보통 자기 자신)
    private LivingEntity _selfLE; // 자기 자신 LivingEntity(자폭 처리용)
    private AudioSource _audioSource;

    void Awake()
    {
        _selfLE = GetComponent<LivingEntity>();
    }

    /// <summary>
    /// 외부에서 폭발 호출. attacker/레이어/반경/데미지 오버라이드 가능.
    /// </summary>
    public void Trigger(int baseDamage, Transform attacker = null,
                        float? radiusOverride = null, LayerMask? maskOverride = null)
    {
        if (oneShot && triggered) return;
        triggered = true;

        _attacker = attacker ? attacker : transform;
        if (radiusOverride.HasValue) radius = Mathf.Max(0.1f, radiusOverride.Value);
        if (maskOverride.HasValue) damageLayers = maskOverride.Value;

        if (delayBeforeExplode > 0f) StartCoroutine(Co_ExplodeAfter(delayBeforeExplode, baseDamage));
        else DoExplode(baseDamage);
    }

    private IEnumerator Co_ExplodeAfter(float t, int baseDamage)
    {
        yield return new WaitForSeconds(t);
        DoExplode(baseDamage);
    }

    private void DoExplode(int baseDamage)
    {
        Vector3 center = transform.position;

        // 1) 데미지 적용
        var hits = Physics.OverlapSphere(center, radius, damageLayers, QueryTriggerInteraction.Ignore);
        var visited = new HashSet<LivingEntity>();

        foreach (var col in hits)
        {
            // 가장 가까운 포인트 기준 거리 계산
            Vector3 p = col.ClosestPoint(center);
            float d = Vector3.Distance(center, p);
            float t = Mathf.Clamp01(d / Mathf.Max(0.0001f, radius));
            float dmgMul = useFalloff ? Mathf.Clamp01(falloff.Evaluate(1f - t)) : 1f;
            float dmg = baseDamage * dmgMul;

            // LivingEntity 피해
            var le = col.GetComponentInParent<LivingEntity>() ?? col.GetComponent<LivingEntity>();
            if (le && !visited.Contains(le))
            {
                visited.Add(le);
                le.OnDamage(dmg, _selfLE ? _selfLE : null);
            }

            // 물리 충격
            var rb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
            if (rb && rb.isKinematic == false)
            {
                rb.AddExplosionForce(explosionForce, center, radius, upwardModifier, ForceMode.Impulse);
            }
        }

        // 2) FX
        if (vfxPrefab) Instantiate(vfxPrefab, center, Quaternion.identity);

        // 3) 자기 제거(풀 반환은 LivingEntity.Die로 연결)
        if (killSelfAfterExplode)
        {
            if (_selfLE) _selfLE.OnDamage(_selfLE.maxHp * 10f, _selfLE); // 즉사 유도
            else Destroy(gameObject);
        }

        // 재사용 가능하게 만들고 싶으면 triggered=false로 되돌리는 옵션 추가 고려
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .5f, 0f, .35f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}
