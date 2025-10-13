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
        range = 20f;
        
        base.Start();
    }

    // Remove all custom behaviors - use base tower system only
}
