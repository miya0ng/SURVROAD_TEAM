using UnityEngine;
using System.Collections.Generic;

public class PooledProjectileSpawn : MonoBehaviour, IProjectileSpawn
{
    public void Spawn(WeaponContext ctx)
    {
        var pool = ObjectPool.GetOrCreate(ctx.Level.bulletPrefab);
        for (int i = 0; i < ctx.Level.ShotCount; i++)
        {
            var bulletObj = pool.Pop(ctx.Muzzle.position, ctx.Muzzle.rotation);
            var b = bulletObj.GetComponent<Bullet>();
            b.Init(ctx.Level.BulletSpeed, ctx.Level.Duration, ctx.Level.MaxDamage, ctx.TeamId, ctx.Owner);
            Debug.Log(ctx.Level.BulletSpeed +"," + ctx.Level.Duration + "," +ctx.Level.MaxDamage + "," +ctx.TeamId + ","+ctx.Owner);
            bulletObj.SetActive(true);
        }
        if (ctx.FireFx) ctx.FireFx.Play();
    }
}