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

    protected override void ConfigureProjectile(ParticleCollisionHandler collisionHandler)
    {
        // Fire Tower - Area Damage
        collisionHandler.towerType = "Fire";
        collisionHandler.areaRadius = 5f;
        collisionHandler.areaMultiplier = 0.5f; // 50% damage to nearby enemies
    }
}
