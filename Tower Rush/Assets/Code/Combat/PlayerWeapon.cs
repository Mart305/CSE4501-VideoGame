using UnityEngine;
using System.Collections;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon Identity")]
    [SerializeField] protected WeaponType weaponType = WeaponType.Pistol;
    [SerializeField] protected string weaponName = "Pistol";

    [Header("Weapon Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float projectileDamage = 30f;
    [SerializeField] private float projectileSpeed = 25f;

    [Header("Ammo Settings")]
    [SerializeField] private bool useAmmo = false;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private float reloadTime = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private float fireSoundVolume = 0.7f;
    [SerializeField] private float reloadSoundVolume = 0.5f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float muzzleFlashDuration = 0.1f;
    [SerializeField] private Light muzzleFlashLight;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 10f;
    [SerializeField] private Color muzzleFlashColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color particleColor = Color.yellow;
    #pragma warning disable 0414 // Field assigned but never used
    private int particleCount = 10;
    private float particleSpeed = 5f;
    #pragma warning restore 0414

    [Header("Animation")]
    [SerializeField] private float recoilAmount = 0.1f;
    [SerializeField] private float recoilSpeed = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f;
    [SerializeField] private AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private ReloadAnimationType reloadAnimType = ReloadAnimationType.Standard;

    [Header("Camera Shake")]
    [SerializeField] private float shakeAmount = 0.05f;
    [SerializeField] private float shakeDuration = 0.1f;
    
    protected float nextFireTime = 0f;
    protected bool isReloading = false;
    protected AudioSource audioSource;
    protected Camera playerCamera;
    protected Vector3 originalPosition;
    protected Quaternion originalRotation;
    protected Coroutine recoilCoroutine;
    protected Coroutine cameraShakeCoroutine;

    private float lastFireSoundTime = 0f;
    private float fireSoundCooldown = 0.05f;
    private WaitForSeconds recoilWait;
    private WaitForSeconds shakeWait;
    
    void Start()
    {
        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (firePoint == null)
        {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0.2f, -0.1f, 0.5f);
            firePoint = firePointObj.transform;
        }

        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        SetupMuzzleFlashLight();

        if (projectilePrefab == null)
        {
            CreateDefaultProjectile();
        }

        // Auto-load weapon sounds if not assigned based on weapon type
        LoadWeaponSpecificAssets();
        ConfigureWeaponVisuals();
    }

    void LoadWeaponSpecificAssets()
    {
        string soundPrefix = GetWeaponSoundPrefix();

        if (fireSound == null)
        {
            fireSound = Resources.Load<AudioClip>($"Sounds/Weapons/{soundPrefix}_fire");
            if (fireSound == null)
            {
                fireSound = Resources.Load<AudioClip>("Sounds/Weapons/weapon_shoot");
            }
            if (fireSound != null)
            {
                Debug.Log($"Loaded {weaponName} fire sound: {fireSound.name}");
            }
        }

        if (reloadSound == null)
        {
            reloadSound = Resources.Load<AudioClip>($"Sounds/Weapons/{soundPrefix}_reload");
            if (reloadSound == null)
            {
                reloadSound = Resources.Load<AudioClip>("Sounds/Weapons/weapon_shoot");
            }
            if (reloadSound != null)
            {
                Debug.Log($"Loaded {weaponName} reload sound: {reloadSound.name}");
            }
        }
    }

    string GetWeaponSoundPrefix()
    {
        switch (weaponType)
        {
            case WeaponType.Pistol: return "pistol";
            case WeaponType.Rifle: return "rifle";
            case WeaponType.Shotgun: return "shotgun";
            case WeaponType.Sniper: return "rifle";
            default: return "weapon";
        }
    }

    void ConfigureWeaponVisuals()
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
                muzzleFlashColor = new Color(1f, 0.9f, 0.5f);
                particleColor = new Color(1f, 0.8f, 0.3f);
                lightIntensity = 1.5f;
                particleCount = 8;
                particleSpeed = 4f;
                recoilAmount = 0.08f;
                shakeAmount = 0.03f;
                reloadAnimType = ReloadAnimationType.Tactical;
                break;

            case WeaponType.Rifle:
                muzzleFlashColor = new Color(1f, 0.6f, 0.2f);
                particleColor = new Color(1f, 0.5f, 0.1f);
                lightIntensity = 2.5f;
                particleCount = 15;
                particleSpeed = 6f;
                recoilAmount = 0.12f;
                shakeAmount = 0.06f;
                reloadAnimType = ReloadAnimationType.Tactical;
                break;

            case WeaponType.Shotgun:
                muzzleFlashColor = new Color(1f, 0.5f, 0f);
                particleColor = new Color(1f, 0.4f, 0f);
                lightIntensity = 3.5f;
                particleCount = 25;
                particleSpeed = 8f;
                recoilAmount = 0.2f;
                shakeAmount = 0.1f;
                reloadAnimType = ReloadAnimationType.Shotgun;
                break;

            case WeaponType.Sniper:
                muzzleFlashColor = new Color(0.8f, 0.9f, 1f);
                particleColor = new Color(0.7f, 0.8f, 1f);
                lightIntensity = 2f;
                particleCount = 12;
                particleSpeed = 7f;
                recoilAmount = 0.25f;
                shakeAmount = 0.12f;
                reloadAnimType = ReloadAnimationType.Sniper;
                break;
        }

        if (muzzleFlashLight != null)
        {
            muzzleFlashLight.color = muzzleFlashColor;
        }
    }
    
    protected virtual void Update()
    {
        if (isReloading)
            return;

        if (useAmmo && currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R) && useAmmo && currentAmmo < maxAmmo)
        {
            StartReload();
        }
    }
    
    public virtual void Fire()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
            
        if (useAmmo && currentAmmo <= 0)
        {
            StartReload();
            return;
        }
        
        nextFireTime = Time.time + fireRate;
        
        Vector3 spawnPosition = firePoint.position;
        Quaternion spawnRotation = Quaternion.LookRotation(playerCamera.transform.forward);
        
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.SetDamage(projectileDamage);
            proj.SetSpeed(projectileSpeed);
        }
        
        PlayFireEffects();
        
        if (useAmmo)
        {
            currentAmmo--;
        }
    }
    
    protected void StartReload()
    {
        if (isReloading || currentAmmo == maxAmmo)
            return;

        isReloading = true;

        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound, reloadSoundVolume);
        }

        StartCoroutine(ReloadAnimation());

        Invoke(nameof(FinishReload), reloadTime);
    }

    protected void FinishReload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
    }
    
    void CreateDefaultProjectile()
    {
        GameObject defaultProjectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        defaultProjectile.name = "DefaultProjectile";
        defaultProjectile.transform.localScale = Vector3.one * 0.1f;
        
        Renderer renderer = defaultProjectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
            renderer.material.SetFloat("_Metallic", 0.8f);
            renderer.material.SetFloat("_Smoothness", 0.8f);
        }
        
        defaultProjectile.AddComponent<Projectile>();
        
        defaultProjectile.layer = LayerMask.NameToLayer("Default");
        
        if (!System.IO.Directory.Exists(Application.dataPath + "/Code/Combat/Prefabs"))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Code/Combat/Prefabs");
        }
        
        projectilePrefab = defaultProjectile;
        defaultProjectile.SetActive(false);
    }
    
    protected virtual void PlayFireEffects()
    {
        if (fireSound != null && audioSource != null && Time.time - lastFireSoundTime > fireSoundCooldown)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(fireSound, fireSoundVolume);
            lastFireSoundTime = Time.time;
        }

        // Always use pooled particles for better performance
        CreateMuzzleFlashParticles();

        if (muzzleFlashLight != null)
        {
            StartCoroutine(MuzzleFlashLight());
        }

        if (recoilCoroutine != null)
            StopCoroutine(recoilCoroutine);
        recoilCoroutine = StartCoroutine(ApplyRecoil());

        if (cameraShakeCoroutine != null)
            StopCoroutine(cameraShakeCoroutine);
        cameraShakeCoroutine = StartCoroutine(CameraShake());
    }

    void CreateMuzzleFlashParticles()
    {
        GameObject particle = WeaponEffectsPool.Instance.GetMuzzleFlashParticle(
            weaponType,
            firePoint.position,
            firePoint.rotation
        );

        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
    }
    
    IEnumerator ApplyRecoil()
    {
        float elapsed = 0f;
        Vector3 targetRecoil = new Vector3(-recoilAmount, Random.Range(-recoilAmount * 0.3f, recoilAmount * 0.3f), 0);
        Quaternion targetRotation = originalRotation * Quaternion.Euler(targetRecoil * 10f);
        
        while (elapsed < 1f / recoilSpeed)
        {
            elapsed += Time.deltaTime;
            float t = recoilCurve.Evaluate(elapsed * recoilSpeed);
            transform.localPosition = Vector3.Lerp(originalPosition, originalPosition + targetRecoil, t);
            transform.localRotation = Quaternion.Slerp(originalRotation, targetRotation, t);
            yield return null;
        }
        
        elapsed = 0f;
        while (elapsed < 1f / recoilRecoverySpeed)
        {
            elapsed += Time.deltaTime;
            float t = recoilCurve.Evaluate(elapsed * recoilRecoverySpeed);
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, t);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, originalRotation, t);
            yield return null;
        }
        
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
    }
    
    IEnumerator CameraShake()
    {
        if (playerCamera == null) yield break;
        
        Vector3 originalCamPos = playerCamera.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeAmount, shakeAmount);
            float y = Random.Range(-shakeAmount, shakeAmount);
            
            playerCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playerCamera.transform.localPosition = originalCamPos;
    }
    
    IEnumerator MuzzleFlashLight()
    {
        muzzleFlashLight.enabled = true;
        float elapsed = 0f;
        float startIntensity = lightIntensity;
        
        while (elapsed < muzzleFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / muzzleFlashDuration;
            muzzleFlashLight.intensity = Mathf.Lerp(startIntensity, 0, t);
            yield return null;
        }
        
        muzzleFlashLight.enabled = false;
    }
    
    IEnumerator ReloadAnimation()
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        switch (reloadAnimType)
        {
            case ReloadAnimationType.Standard:
                yield return StandardReloadAnimation(startPos);
                break;

            case ReloadAnimationType.Tactical:
                yield return TacticalReloadAnimation(startPos, startRot);
                break;

            case ReloadAnimationType.Shotgun:
                yield return ShotgunReloadAnimation(startPos, startRot);
                break;

            case ReloadAnimationType.Sniper:
                yield return SniperReloadAnimation(startPos, startRot);
                break;
        }

        transform.localPosition = startPos;
        transform.localRotation = startRot;
    }

    IEnumerator StandardReloadAnimation(Vector3 startPos)
    {
        Vector3 reloadPos = startPos + new Vector3(0, -0.2f, 0);
        float elapsed = 0f;

        while (elapsed < reloadTime * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.3f);
            transform.localPosition = Vector3.Lerp(startPos, reloadPos, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < reloadTime * 0.7f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.7f);
            transform.localPosition = Vector3.Lerp(reloadPos, startPos, t);
            yield return null;
        }
    }

    IEnumerator TacticalReloadAnimation(Vector3 startPos, Quaternion startRot)
    {
        Vector3 dropPos = startPos + new Vector3(-0.15f, -0.3f, 0);
        Vector3 insertPos = startPos + new Vector3(0.15f, -0.25f, 0);
        Quaternion tiltRot = startRot * Quaternion.Euler(0, 0, -30f);
        float elapsed = 0f;

        // Drop mag motion
        while (elapsed < reloadTime * 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.2f);
            transform.localPosition = Vector3.Lerp(startPos, dropPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, tiltRot, t);
            yield return null;
        }

        // Insert mag motion
        elapsed = 0f;
        while (elapsed < reloadTime * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.3f);
            transform.localPosition = Vector3.Lerp(dropPos, insertPos, t);
            yield return null;
        }

        // Return to position
        elapsed = 0f;
        while (elapsed < reloadTime * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.5f);
            transform.localPosition = Vector3.Lerp(insertPos, startPos, t);
            transform.localRotation = Quaternion.Slerp(tiltRot, startRot, t);
            yield return null;
        }
    }

    IEnumerator ShotgunReloadAnimation(Vector3 startPos, Quaternion startRot)
    {
        Vector3 tiltPos = startPos + new Vector3(0.1f, -0.15f, -0.1f);
        Quaternion tiltRot = startRot * Quaternion.Euler(45f, 0, 15f);
        int shellsToLoad = 3;
        float shellLoadTime = reloadTime / (shellsToLoad + 1);

        // Tilt weapon down
        float elapsed = 0f;
        while (elapsed < shellLoadTime * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (shellLoadTime * 0.5f);
            transform.localPosition = Vector3.Lerp(startPos, tiltPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, tiltRot, t);
            yield return null;
        }

        // Load shells one by one
        for (int i = 0; i < shellsToLoad; i++)
        {
            Vector3 insertPos = tiltPos + new Vector3(0, -0.05f, 0);

            elapsed = 0f;
            while (elapsed < shellLoadTime * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 10f, 1f);
                transform.localPosition = Vector3.Lerp(tiltPos, insertPos, t * 0.5f);
                yield return null;
            }
        }

        // Return to position
        elapsed = 0f;
        while (elapsed < shellLoadTime * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (shellLoadTime * 0.5f);
            transform.localPosition = Vector3.Lerp(tiltPos, startPos, t);
            transform.localRotation = Quaternion.Slerp(tiltRot, startRot, t);
            yield return null;
        }
    }

    IEnumerator SniperReloadAnimation(Vector3 startPos, Quaternion startRot)
    {
        Vector3 boltPos = startPos + new Vector3(0.2f, -0.1f, -0.15f);
        Vector3 magPos = startPos + new Vector3(0, -0.35f, 0);
        Quaternion boltRot = startRot * Quaternion.Euler(-15f, 10f, -20f);
        float elapsed = 0f;

        // Pull bolt back
        while (elapsed < reloadTime * 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.2f);
            transform.localPosition = Vector3.Lerp(startPos, boltPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, boltRot, t);
            yield return null;
        }

        // Drop mag
        elapsed = 0f;
        while (elapsed < reloadTime * 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.15f);
            transform.localPosition = Vector3.Lerp(boltPos, magPos, t);
            yield return null;
        }

        // Insert new mag
        elapsed = 0f;
        while (elapsed < reloadTime * 0.25f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.25f);
            transform.localPosition = Vector3.Lerp(magPos, boltPos, t);
            yield return null;
        }

        // Push bolt forward and return
        elapsed = 0f;
        while (elapsed < reloadTime * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (reloadTime * 0.4f);
            transform.localPosition = Vector3.Lerp(boltPos, startPos, t);
            transform.localRotation = Quaternion.Slerp(boltRot, startRot, t);
            yield return null;
        }
    }
    
    void SetupMuzzleFlashLight()
    {
        if (muzzleFlashLight == null && firePoint != null)
        {
            GameObject lightObj = new GameObject("MuzzleFlashLight");
            lightObj.transform.SetParent(firePoint);
            lightObj.transform.localPosition = Vector3.zero;
            muzzleFlashLight = lightObj.AddComponent<Light>();
            muzzleFlashLight.type = LightType.Point;
            muzzleFlashLight.color = muzzleFlashColor;
            muzzleFlashLight.intensity = 0;
            muzzleFlashLight.range = lightRange;
            muzzleFlashLight.enabled = false;
        }
    }
    
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;
    public WeaponType GetWeaponType() => weaponType;
    public string GetWeaponName() => weaponName;
    public Color GetMuzzleFlashColor() => muzzleFlashColor;
}