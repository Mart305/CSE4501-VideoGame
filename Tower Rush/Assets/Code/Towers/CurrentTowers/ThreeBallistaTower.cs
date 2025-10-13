using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeBallistaTower : BaseTower
{
    public override void Start()
    {
        // Set three ballista tower specific stats
        damage = 35f;
        fireRate = 1.2f;
        range = 20f;
        
        base.Start();
    }

    // Remove all custom behaviors - use base tower system only

}
