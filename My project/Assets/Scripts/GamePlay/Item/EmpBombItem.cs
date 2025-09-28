using UnityEngine;

public class EmpBombItem : ItemBase
{
    protected override void Collect(GameObject player)
    {
        float dur = itemData.Duration > 0 ? itemData.Duration : 2f;
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var stun = e.GetComponent<StunOnRigidbody>();
            if (stun) stun.Stun(dur);
        }
        Destroy(gameObject);
    }
}