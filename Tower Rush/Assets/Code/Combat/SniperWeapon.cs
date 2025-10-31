using UnityEngine;

public class SniperWeapon : PlayerWeapon
{
    [Header("Sniper Specific Settings")]
    [SerializeField] private float scopeZoomFOV = 20f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float sniperDamageMultiplier = 2.5f;

    private Camera playerCam;
    private bool isScoped = false;

    void Awake()
    {
        weaponType = WeaponType.Sniper;
        weaponName = "Sniper Rifle";

        playerCam = Camera.main;
        if (playerCam == null)
        {
            playerCam = GetComponentInChildren<Camera>();
        }
    }

    void Update()
    {
        base.Update();

        if (Input.GetMouseButtonDown(1))
        {
            isScoped = !isScoped;
        }

        if (playerCam != null)
        {
            float targetFOV = isScoped ? scopeZoomFOV : normalFOV;
            playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }
    }

    public override void Fire()
    {
        base.Fire();

        if (isScoped)
        {
            isScoped = false;
        }
    }
}
