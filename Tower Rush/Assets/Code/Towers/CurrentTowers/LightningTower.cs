using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningTower : BaseTower
{
    public override void Start()
    {
        // Set lightning tower specific stats
        damage = 40f;
        fireRate = 1f;
        range = 20f;
        
        base.Start();
    }

    // Remove all custom behaviors - use base tower system only
}
