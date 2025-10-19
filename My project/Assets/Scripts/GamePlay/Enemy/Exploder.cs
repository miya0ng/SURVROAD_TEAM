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
    [SerializeField] private AudioClip sfxClip;

    [Header("Options")]
    [SerializeField] private bool killSelfAfterExplode = true; // 자기 제거(= 풀 반환 트리거)
    [SerializeField] private float delayBeforeExplode = 0f;    // 점화 후 지연
    [SerializeField] private bool oneShot = true;              // 중복 방지

    // === 보강 옵션 ===
    [Header("Team/Friendly-Fire")]
    [SerializeField] private bool ignoreSelf = true;        // 자기 자신 피해 무시
    [SerializeField] private bool ignoreTeam = true;    // 동일 팀 피해 무시

    [Header("Perf/Limit")]
    [SerializeField] private int maxVictims = 64;         // 최대 타격 대상 수(과도한 연산 안전장치)
    [SerializeField] private bool useNonAlloc = true;      // OverlapSphereNonAlloc 사용
    [SerializeField] private int nonAllocBuffer = 128;    // 버퍼 크기

    private bool triggered;
    private Transform _attacker;     // 데미지 가해자(보통 자기 자신 Transform)
    private LivingEntity _selfLE;    // 자기 자신 LivingEntity(자폭 처리용)
    private AudioSource _audioSource;

    // NonAlloc 버퍼
    private Collider[] _hits;

    void Awake()
    {
        _selfLE = GetComponent<LivingEntity>();
        if (useNonAlloc) _hits = new Collider[Mathf.Max(8, nonAllocBuffer)];
        if (!_audioSource && (sfxClip != null))
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.maxDistance = radius * 3f;
        }
    }

    /// <summary>
    /// 외부에서 폭발 호출. attacker/레이어/반경/데미지 오버라이드 가능.
    /// 기존 시그니처 유지.
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

    // 오버로드: CSV 스펙과 바로 연결할 때 사용 가능
    public void Trigger(EnemySpec spec, Transform attacker = null,
                        float? radiusOverride = null, LayerMask? maskOverride = null)
    {
        Trigger(Mathf.Max(1, spec.AttackDamage), attacker, radiusOverride, maskOverride);
    }

    private IEnumerator Co_ExplodeAfter(float t, int baseDamage)
    {
        yield return new WaitForSeconds(t);
        DoExplode(baseDamage);
    }

    private void DoExplode(int baseDamage)
    {
        Vector3 center = transform.position;

        // 오디오
        if (_audioSource && sfxClip)
        {
            _audioSource.volume = sfxVolume;
            _audioSource.PlayOneShot(sfxClip);
        }

        // 1) 물체 수집
        int count = 0;
        Collider[] cols;
        if (useNonAlloc)
        {
            count = Physics.OverlapSphereNonAlloc(center, radius, _hits, damageLayers, QueryTriggerInteraction.Ignore);
            cols = _hits;
        }
        else
        {
            cols = Physics.OverlapSphere(center, radius, damageLayers, QueryTriggerInteraction.Ignore);
            count = cols.Length;
        }

        var visited = new HashSet<LivingEntity>();
        int victims = 0;

        for (int i = 0; i < count; i++)
        {
            if (victims >= maxVictims) break;
            var col = cols[i];
            if (!col) continue;

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
                // 자기 자신/동일 팀 무시 옵션
                if (ignoreSelf && _selfLE && le == _selfLE) goto PHYSICS_ONLY;
                if (ignoreTeam && _selfLE && le.teamId == _selfLE.teamId) goto PHYSICS_ONLY;

                visited.Add(le);
                le.OnDamage(dmg, _selfLE ? _selfLE : null);
                victims++;
            }

        PHYSICS_ONLY:
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

        // 재사용형으로 바꾸고 싶다면 oneShot=false + triggered=false 리셋을 외부에서 처리
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .5f, 0f, .35f);
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif
}
