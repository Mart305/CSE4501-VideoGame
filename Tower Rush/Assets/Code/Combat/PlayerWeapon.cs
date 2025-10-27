using UnityEngine;
using System.Collections;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;  // Improved from 0.3f for smoother shooting
    [SerializeField] private float projectileDamage = 30f;  // Increased from 25f for more rewarding hits
    [SerializeField] private float projectileSpeed = 25f;  // Increased from 20f for better responsiveness
    
    [Header("Ammo Settings")]
    [SerializeField] private bool useAmmo = false;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private float reloadTime = 1.5f;  // Reduced from 2f for faster gameplay flow
    
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
    
    [Header("Animation")]
    [SerializeField] private float recoilAmount = 0.1f;
    [SerializeField] private float recoilSpeed = 10f;
    [SerializeField] private float recoilRecoverySpeed = 5f;
    [SerializeField] private AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
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

        // Auto-load weapon sounds if not assigned
        if (fireSound == null)
        {
            fireSound = Resources.Load<AudioClip>("Sounds/Weapons/weapon_shoot");
        }

        if (reloadSound == null)
        {
            reloadSound = Resources.Load<AudioClip>("Sounds/Weapons/weapon_shoot");
        }
    }
    
    void Update()
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
        if (fireSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(fireSound, fireSoundVolume);
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            flash.transform.SetParent(firePoint);
            Destroy(flash, muzzleFlashDuration);
        }

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
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 reloadPos = startPos + new Vector3(0, -0.2f, 0);
        
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
        
        transform.localPosition = startPos;
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
}