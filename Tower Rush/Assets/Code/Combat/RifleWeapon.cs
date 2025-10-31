using UnityEngine;

public class RifleWeapon : PlayerWeapon
{
    [Header("Rifle Specific Settings")]
    [SerializeField] private bool burstFire = false;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.1f;

    void Awake()
    {
        weaponType = WeaponType.Rifle;
        weaponName = "Assault Rifle";
    }

    public override void Fire()
    {
        if (burstFire)
        {
            StartCoroutine(FireBurst());
        }
        else
        {
            base.Fire();
        }
    }

    System.Collections.IEnumerator FireBurst()
    {
        for (int i = 0; i < burstCount; i++)
        {
            if (GetCurrentAmmo() <= 0 || IsReloading())
                break;

            base.Fire();
            yield return new System.Collections.WaitForSeconds(burstDelay);
        }
    }
}
