using System.Collections.Generic;
using UnityEngine;

public class BallistaTower : BaseTower
{
    // Ballista Tower - High Damage
    // This tower deals high single-target damage with slower fire rate
    // Perfect for taking down tough enemies
    
    public override void Start()
    {
        // Set ballista tower specific stats - HIGH DAMAGE
        damage = 80f; // Increased from 50 to emphasize high damage
        fireRate = 0.6f; // Slower but very powerful
        range = 25f; // Slightly longer range
        
        base.Start();
    }
}
