using System.Collections.Generic;
using UnityEngine;

public class BallistaTower : BaseTower
{
    public override void Start()
    {
        // Set ballista tower specific stats
        damage = 50f;
        fireRate = 0.8f; // Slower but powerful
        range = 20f;
        
        base.Start();
    }

    // Remove all custom behaviors - use base tower system only
}
