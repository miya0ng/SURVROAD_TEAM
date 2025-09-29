using System;
using UnityEngine;

public class WeaponDriver : WeaponBase
{
    [Header("Wiring")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem fireEffect;

    private IAimStrategy aim;
    private IFireStrategy fire;
    private IProjectileSpawn spawn;

    public event Action<int, WeaponLevelData> OnLevelChanged;
    private bool isGunType;

    void Awake()
    {
        aim = GetComponent<IAimStrategy>();
        fire = GetComponent<IFireStrategy>();
        spawn = GetComponent<IProjectileSpawn>();
    }

    public override bool SetLevel(int level)
    {
        var ok = base.SetLevel(level);
        if (!ok || CurLevelData == null) return false;

        OnLevelChanged?.Invoke(CurLevel, CurLevelData);

        // 총기류만 조준 사용 (Type 1/2)
        isGunType = (weaponSO != null) && (weaponSO.Type == 1 || weaponSO.Type == 2 || weaponSO.Type == 3);
        fire?.Reset();
        return true;
    }

    void Update()
    {
        if (weaponSO == null || CurLevelData == null) return;

        // 1) 조준은 총기류만 (트랩/근접은 조준 불필요)
        if (isGunType && aim != null)
        {
            var target = aim.GetAimTarget(transform, teamId);
            if (target != null)
                transform.rotation = aim.GetRotationTowards(transform, target);
        }

        // 2) 발사는 무기 타입 관계없이 IFireStrategy가 붙어 있으면 실행

        if (fire != null && fire.ShouldFire(Time.deltaTime, CurLevelData))
        {
            spawn?.Spawn(BuildCtx());
        }
    }

    private WeaponContext BuildCtx() => new WeaponContext
    {
        Muzzle = muzzle != null ? muzzle : transform,
        Level = CurLevelData,
        TeamId = teamId,
        Owner = ownerEntity,
        FireFx = fireEffect
    };
}
