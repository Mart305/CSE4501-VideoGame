using UnityEngine;

public class Zombie : Enemy
{
    protected override void Start()
    {
        // Zombie stats
        moveSpeed = 1.5f;
        health = 50f;
        damage = 15f;
        attackCooldown = 2f;

        base.Start();
    }
}
