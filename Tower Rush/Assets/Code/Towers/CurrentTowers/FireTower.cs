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

    // Remove all custom behaviors - use base tower system only
}
