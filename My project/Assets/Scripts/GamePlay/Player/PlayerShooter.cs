using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    public WeaponSO weaponsSO { get; set; }
    private Weapon weapon {  get; set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        weapon = GetComponent<Weapon>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EquipWeapon(WeaponSO w)
    {
        this.weaponsSO = w;
        weapon.weaponSO= weaponsSO;
    }
}
