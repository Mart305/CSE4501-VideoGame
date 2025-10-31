using UnityEngine;

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper
}

public enum ReloadAnimationType
{
    Standard,      // Simple down-up motion
    Tactical,      // Quick mag drop and insert
    Shotgun,       // Shell-by-shell loading
    Sniper         // Slow, deliberate reload
}

[System.Serializable]
public class WeaponVisualConfig
{
    public Color muzzleFlashColor = Color.yellow;
    public Color particleColor = Color.yellow;
    public float muzzleFlashIntensity = 2f;
    public float muzzleFlashDuration = 0.1f;
    public int particleCount = 10;
    public float particleSpeed = 5f;
    public float particleSize = 0.1f;
    public float cameraShakeAmount = 0.05f;
    public float recoilAmount = 0.1f;
}
