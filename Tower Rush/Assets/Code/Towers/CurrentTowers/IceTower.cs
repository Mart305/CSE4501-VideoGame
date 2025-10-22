using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceTower : BaseTower
{
    public override void Start()
    {
        // Set ice tower specific stats
        damage = 20f;
        fireRate = 2f;
        // Don't set range here - let base.Start() calculate it from projectile
        
        base.Start();
        
        // Optionally override range here if needed (after base.Start())
        // range = 20f;
    }

    // Remove all custom behaviors - use base tower system only
}
