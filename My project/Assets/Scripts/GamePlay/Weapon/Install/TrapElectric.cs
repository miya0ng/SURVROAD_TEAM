using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class TrapElectric : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] SphereCollider zone;      // isTrigger = true
    [SerializeField] LayerMask enemyMask = ~0; // Enemy ���̾

    [Header("Effects")]
    [Tooltip("Lv1 ���ο� ���(0.4=60%����)")]
    [Range(0.05f, 1f)]
    [SerializeField] float slowAtLv1 = 0.40f;
    [Tooltip("Lv5 ���ο� ���(0.1=90%����)")]
    [Range(0.05f, 1f)]
    [SerializeField] float slowAtLv5 = 0.10f;
    [SerializeField] float tickInterval = 0.25f;

    // �� ���� ��ƼŬ�� ���δ� �θ� ���� (�ڽĿ� ParticleSystem 2�� �̻�)
    [SerializeField] GameObject fxLoopRoot;
    // ���� ����(���� ��)�� ��Ʈ�� �Ǵٸ� �ڽ��̸� �̷���
    [SerializeField] ParticleSystem fxEnd;  // �ʿ� ������ ���� ����
    ParticleSystem[] _loopSystems;
    // ����
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

        // ������ �������� �� ���� ���ο�(1~5 ����)
        int level = Mathf.Max(1, lv.Level);
        float t = Mathf.Clamp01((level - 1) / 4f); // 1��5�� 0��1�� ����ȭ
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
        //    // Ǯ ��ü�� �ٷ� ��Ȱ��ȭ�ŵ� ���� ����Ʈ�� ���̵���, ��� ��� �� ���
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
    // 레이어 체크 (비트마스크 연산)
    if (((1 << other.gameObject.layer) & enemyMask) == 0) return;
    
    var le = other.GetComponentInParent<LivingEntity>();
    if (!IsValid(le)) return;

    if (_inside.Add(le))
    {
        var token = le.GetComponent<TrapSlowToken>();
        if (!token)
        {
            token = le.gameObject.AddComponent<TrapSlowToken>();
            
            if (le.GetComponent<IExternalSpeedScale>() == null)
            {
                Debug.LogWarning($"[TrapElectric] {le.name}에 IExternalSpeedScale 미구현");
            }
        }
        
        token.AddRef(_slowMult);
        
        Debug.Log($"[TrapElectric] {le.name} 감속 적용: {_slowMult:F2} (레벨 {_slowMult})");
    }
}

void OnTriggerExit(Collider other)
{
    var le = other.GetComponentInParent<LivingEntity>();
    if (!le) return;
    
    if (_inside.Remove(le))
    {
        var token = le.GetComponent<TrapSlowToken>();
        if (token != null)
        {
            token.RemoveRef(_slowMult);
            Debug.Log($"[TrapElectric] {le.name} 감속 해제");
        }
    }
}

    bool IsValid(LivingEntity le) => le && le != _owner && le.teamId != _team;

    void CleanupTokens()
    {
        foreach (var le in _inside)
            if (le) le.GetComponent<TrapSlowToken>()?.RemoveAll();
        _inside.Clear();
    }
}
