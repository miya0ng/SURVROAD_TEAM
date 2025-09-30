using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TrapElectric : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] SphereCollider zone;      // isTrigger = true
    [SerializeField] LayerMask enemyMask = ~0; // Enemy 레이어만

    [Header("Effects")]
    [Tooltip("Lv1 슬로우 배수(0.4=60%감속)")]
    [Range(0.05f, 1f)]
    [SerializeField] float slowAtLv1 = 0.40f;
    [Tooltip("Lv5 슬로우 배수(0.1=90%감속)")]
    [Range(0.05f, 1f)]
    [SerializeField] float slowAtLv5 = 0.10f;
    [SerializeField] float tickInterval = 0.25f;

    // 두 루프 파티클을 감싸는 부모만 참조 (자식에 ParticleSystem 2개 이상)
    [SerializeField] GameObject fxLoopRoot;
    // 종료 연출(폭발 등)이 루트의 또다른 자식이면 이렇게
    [SerializeField] ParticleSystem fxEnd;  // 필요 없으면 제거 가능
    ParticleSystem[] _loopSystems;
    // 주입
    private LivingEntity _owner;
    private TeamId _team;
    private float _minDmg, _maxDmg;
    private float _duration, _slowMult;

    private readonly HashSet<LivingEntity> _inside = new();
    private float _tLife, _tTick;
    void Awake()
    {
        if (fxLoopRoot)
            _loopSystems = fxLoopRoot.GetComponentsInChildren<ParticleSystem>(true);
        else
            _loopSystems = System.Array.Empty<ParticleSystem>();
    }
    private void Start()
    {
        var check = GetComponentInParent<EquipSocket>();
        if (check != null)
        {
            var effects = GameObject.FindGameObjectsWithTag("VFX");
            foreach (var effect in effects)
                effect.SetActive(false);
        }
    }
    public void Init(LivingEntity owner, TeamId team, WeaponLevelData lv)
    {
        _owner = owner; _team = team;

        _minDmg = Mathf.Max(0, lv.MinDamage);
        _maxDmg = Mathf.Max(_minDmg, lv.MaxDamage);
        _duration = Mathf.Max(0.5f, lv.Duration);

        if (!zone) zone = GetComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = Mathf.Max(0.5f, lv.EffectiveRange);

        // 레벨이 높을수록 더 강한 슬로우(1~5 가정)
        int level = Mathf.Max(1, lv.Level);
        float t = Mathf.Clamp01((level - 1) / 4f); // 1→5를 0→1로 정규화
        _slowMult = Mathf.Lerp(slowAtLv1, slowAtLv5, t);

        _tLife = _tTick = 0f;
        PlayLoops();
    }

    void OnEnable() { _inside.Clear(); _tLife = _tTick = 0f; PlayLoops(); }
    void OnDisable() { CleanupTokens(); StopLoops(); }
    void PlayLoops()
    {
        if (_loopSystems == null) return;
        for (int i = 0; i < _loopSystems.Length; i++)
            if (_loopSystems[i]) _loopSystems[i].Play(true);
    }

    void StopLoops()
    {
        if (_loopSystems == null) return;
        for (int i = 0; i < _loopSystems.Length; i++)
            if (_loopSystems[i]) _loopSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    void CleanupAndDespawn()
    {
        CleanupTokens();

        //if (fxEnd)
        //{
        //    // 풀 객체가 바로 비활성화돼도 엔딩 이펙트가 보이도록, 잠깐 떼어낸 뒤 재생
        //    var end = fxEnd;
        //    end.transform.SetParent(null, true);
        //    end.transform.position = transform.position;
        //    end.Play(true);
        //    Destroy(end.gameObject, end.main.duration + end.main.startLifetime.constantMax);
        //}
        if (fxLoopRoot)
        {
            Destroy(fxLoopRoot);
        }

        //gameObject.SetActive(false);
        Destroy(gameObject);
    }
    void Update()
    {
        _tLife += Time.deltaTime;
        _tTick += Time.deltaTime;

        if (_tTick >= tickInterval)
        {
            _tTick = 0f;
            foreach (var le in _inside)
            {
                if (!le || le.teamId == _team || le == _owner) continue;
                le.OnDamage(Random.Range(_minDmg, _maxDmg + 1), _owner);
            }
        }

        if (_tLife >= _duration && _duration != 0)
        {
            if (fxEnd) Instantiate(fxEnd, transform.position, Quaternion.identity);
            CleanupAndDespawn();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & enemyMask) == 0) return;
        var le = other.GetComponentInParent<LivingEntity>();
        if (!IsValid(le)) return;

        if (_inside.Add(le))
        {
            var token = le.GetComponent<TrapSlowToken>();
            if (!token) token = le.gameObject.AddComponent<TrapSlowToken>();
            token.AddRef(_slowMult);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var le = other.GetComponentInParent<LivingEntity>();
        if (!le) return;
        if (_inside.Remove(le))
            le.GetComponent<TrapSlowToken>()?.RemoveRef(_slowMult);
    }

    bool IsValid(LivingEntity le) => le && le != _owner && le.teamId != _team;

    void CleanupTokens()
    {
        foreach (var le in _inside)
            if (le) le.GetComponent<TrapSlowToken>()?.RemoveAll();
        _inside.Clear();
    }
}
