using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidTower : BaseTower
{
    public override void Start()
    {
        // Set void tower specific stats
        damage = 25f; // Standard damage like other towers
        fireRate = 1f; 
        range = 20f;
        
        base.Start();
    }

    // Remove all custom behaviors - use base tower system only

}
