using UnityEngine;


public enum WeaponIndex
{
    SM_Prop_CarBattery_02,
    SM_Wep_Veh_MachineGun_01,
    SM_Wep_AAGun_01,
    SM_Wep_Veh_MiniGun_01,
    SM_Wep_Veh_Rocket_Launcher_01,
    SM_Wep_Veh_Saw_Launcher_Ammo_01,
    SM_Wep_Melee_Spear_Wood_01,
    SM_Wep_Bomb_Propane_01,
}

public enum ItemType
{
    NormalPart,   // 플레이 도중 강화용
    SpecialPart,  // 게임 종료 후 업그레이드용
    Heal,         // 수리키트 같은 소모 아이템
    Booster          // 부스터
}