using UnityEngine;

/// <summary>
/// Centralized game balance configuration
/// Adjust these values to balance difficulty, progression, and economy
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Tower Rush/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("=== ENEMY BALANCE ===")]
    
    [Header("Zombie")]
    [Tooltip("Base health for zombie enemies")]
    public float zombieBaseHealth = 120f;
    [Tooltip("Health multiplier per wave (1.1 = 10% increase per wave)")]
    public float zombieHealthScaling = 1.08f;
    [Tooltip("Movement speed")]
    public float zombieSpeed = 3.5f;
    [Tooltip("Damage to towers")]
    public float zombieDamage = 15f;
    
    [Header("Ghost")]
    public float ghostBaseHealth = 90f;
    public float ghostHealthScaling = 1.1f;
    public float ghostSpeed = 5.5f;
    public float ghostDamage = 12f;
    
    [Header("Mutant Zombie")]
    public float mutantBaseHealth = 250f;
    public float mutantHealthScaling = 1.12f;
    public float mutantSpeed = 2.8f;
    public float mutantDamage = 25f;
    
    [Header("Skeleton")]
    public float skeletonBaseHealth = 100f;
    public float skeletonHealthScaling = 1.09f;
    public float skeletonSpeed = 4.2f;
    public float skeletonDamage = 18f;
    
    [Header("=== TOWER BALANCE ===")]
    
    [Header("Fire Tower")]
    public float fireTowerDamage = 30f;
    public float fireTowerRange = 12f;
    public float fireTowerFireRate = 1.2f; // Attacks per second
    
    [Header("Ice Tower")]
    public float iceTowerDamage = 20f;
    public float iceTowerRange = 14f;
    public float iceTowerFireRate = 1f;
    public float iceTowerSlowAmount = 0.6f; // 60% slow
    
    [Header("Lightning Tower")]
    public float lightningTowerDamage = 50f;
    public float lightningTowerRange = 10f;
    public float lightningTowerFireRate = 0.7f;
    public int lightningChainTargets = 3;
    
    [Header("Ballista Tower")]
    public float ballistaTowerDamage = 75f;
    public float ballistaTowerRange = 18f;
    public float ballistaTowerFireRate = 0.4f;
    
    [Header("Three Ballista Tower")]
    public float threeBallistaDamage = 55f;
    public float threeBalistaRange = 15f;
    public float threeBalistaFireRate = 0.5f;
    
    [Header("Void Tower")]
    public float voidTowerDamage = 35f;
    public float voidTowerRange = 12f;
    public float voidTowerFireRate = 0.8f;
    public float voidTowerDOTDamage = 8f; // Damage over time per second
    
    [Header("=== CURRENCY BALANCE ===")]
    
    [Header("Starting Currency")]
    public int startingGold = 300;
    
    [Header("Enemy Rewards")]
    [Tooltip("Gold earned per zombie kill")]
    public int zombieReward = 15;
    public int ghostReward = 18;
    public int mutantReward = 35;
    public int skeletonReward = 20;
    
    [Header("Wave Completion Bonus")]
    [Tooltip("Base gold bonus for completing a wave")]
    public int waveCompletionBonus = 75;
    [Tooltip("Additional gold per wave number (wave 5 = 75 + 5*15 = 150 gold)")]
    public int bonusPerWaveNumber = 15;
    
    [Header("=== UPGRADE COSTS ===")]
    
    [Header("Tower Upgrades")]
    public int repairCost = 60;
    public int maxHealthUpgradeCost = 120;
    public int damageResistanceUpgradeCost = 180;
    
    [Header("Upgrade Scaling")]
    [Tooltip("Cost multiplier for each upgrade level (1.5 = 50% more expensive each time)")]
    public float upgradeCostScaling = 1.4f;
    
    [Header("=== WAVE BALANCE ===")]
    
    [Header("Wave Progression")]
    [Tooltip("Base number of enemies in wave 1")]
    public int baseEnemiesPerWave = 12;
    [Tooltip("Additional enemies per wave")]
    public int enemiesIncreasePerWave = 3;
    [Tooltip("Time between enemy spawns in seconds")]
    public float spawnDelay = 1.5f;
    [Tooltip("Time between waves in seconds")]
    public float timeBetweenWaves = 20f;
    
    [Header("Difficulty Curve")]
    [Tooltip("Wave when elite enemies start appearing")]
    public int eliteEnemyStartWave = 3;
    [Tooltip("Wave when boss enemies start appearing")]
    public int bossEnemyStartWave = 7;
    
    [Header("=== TOWER HEALTH ===")]
    
    [Header("Tower Durability")]
    public float baseTowerHealth = 600f;
    public float baseTowerMaxHealth = 600f;
    public float baseDamageResistance = 0f; // 0-0.8 (0% to 80%)
    
    [Header("Health Upgrades")]
    public float maxHealthIncreasePerUpgrade = 150f;
    public float damageResistanceIncreasePerUpgrade = 0.12f; // 12% per upgrade
    public float maxDamageResistance = 0.75f; // 75% max
    
    // Helper methods for easy access
    public float GetEnemyHealth(string enemyType, int waveNumber)
    {
        float baseHealth = 0f;
        float scaling = 1f;
        
        switch (enemyType.ToLower())
        {
            case "zombie":
                baseHealth = zombieBaseHealth;
                scaling = zombieHealthScaling;
                break;
            case "ghost":
                baseHealth = ghostBaseHealth;
                scaling = ghostHealthScaling;
                break;
            case "mutantzombie":
            case "mutant":
                baseHealth = mutantBaseHealth;
                scaling = mutantHealthScaling;
                break;
            case "skeleton":
                baseHealth = skeletonBaseHealth;
                scaling = skeletonHealthScaling;
                break;
        }
        
        return baseHealth * Mathf.Pow(scaling, waveNumber - 1);
    }
    
    public int GetEnemyReward(string enemyType)
    {
        switch (enemyType.ToLower())
        {
            case "zombie":
                return zombieReward;
            case "ghost":
                return ghostReward;
            case "mutantzombie":
            case "mutant":
                return mutantReward;
            case "skeleton":
                return skeletonReward;
            default:
                return zombieReward;
        }
    }
    
    public int GetWaveCompletionReward(int waveNumber)
    {
        return waveCompletionBonus + (waveNumber * bonusPerWaveNumber);
    }
    
    public int GetUpgradeCost(string upgradeType, int currentLevel)
    {
        int baseCost = 0;
        
        switch (upgradeType.ToLower())
        {
            case "repair":
                baseCost = repairCost;
                break;
            case "maxhealth":
                baseCost = maxHealthUpgradeCost;
                break;
            case "resistance":
                baseCost = damageResistanceUpgradeCost;
                break;
        }
        
        return Mathf.RoundToInt(baseCost * Mathf.Pow(upgradeCostScaling, currentLevel));
    }
    
    public int GetEnemiesForWave(int waveNumber)
    {
        return baseEnemiesPerWave + (waveNumber - 1) * enemiesIncreasePerWave;
    }
}
