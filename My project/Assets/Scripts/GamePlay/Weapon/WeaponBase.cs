using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public WeaponSO weaponSO;

    public WeaponLevelData CurLevelData { get; protected set; }
    public int CurLevel { get; protected set; } = 1;

    protected LivingEntity ownerEntity;
    protected TeamId teamId;

    public virtual void Init(LivingEntity owner, WeaponSO so, int startLevel = 1)
    {
        ownerEntity = owner;
        teamId = owner.teamId;
        weaponSO = so;
        SetLevel(startLevel);
    }

    public virtual bool SetLevel(int level)
    {
        CurLevelData = weaponSO.Levels.Find(l => l.Level == level);
        if (CurLevelData == null) return false;
        CurLevel = level;
        return true;
    }
}