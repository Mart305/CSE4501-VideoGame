using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float projectileDamage = 25f;
    [SerializeField] private float projectileSpeed = 20f;
    
    [Header("Ammo Settings")]
    [SerializeField] private bool useAmmo = false;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private float reloadTime = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;
    
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private AudioSource audioSource;
    private Camera playerCamera;
    
    void Start()
    {
        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        
        if (projectilePrefab == null)
        {
            CreateDefaultProjectile();
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
    
    public void Fire()
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
        
        if (fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
        
        if (useAmmo)
        {
            currentAmmo--;
        }
    }
    
    void StartReload()
    {
        if (isReloading || currentAmmo == maxAmmo)
            return;
            
        isReloading = true;
        
        if (reloadSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }
        
        Invoke(nameof(FinishReload), reloadTime);
    }
    
    void FinishReload()
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
        
        string prefabPath = "Assets/Code/Combat/Prefabs";
        if (!System.IO.Directory.Exists(Application.dataPath + "/Code/Combat/Prefabs"))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Code/Combat/Prefabs");
        }
        
        projectilePrefab = defaultProjectile;
        defaultProjectile.SetActive(false);
    }
    
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;
}