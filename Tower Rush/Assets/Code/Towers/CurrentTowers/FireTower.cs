using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTower : BaseTower
{
    public override void Start()
    {
        // Set fire tower specific stats
        damage = 30f;
        fireRate = 1.5f;
        range = 20f;
        
        base.Start();
    }

    protected override void ConfigureProjectile(SimpleProjectile projectile)
    {
        // Fire Tower - Area Damage
        projectile.towerType = "Fire";
        projectile.areaRadius = 5f;
        projectile.areaMultiplier = 0.5f; // 50% damage to nearby enemies
    }
}
