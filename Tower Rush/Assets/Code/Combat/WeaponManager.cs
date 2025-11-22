using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private PlayerWeapon primaryWeapon;
    [SerializeField] private ShotgunWeapon secondaryWeapon;
    [SerializeField] private WeaponIdentifier weaponIdentifier;

    [Header("Input Settings")]
    [SerializeField] private float weaponSwitchCooldown = 0.3f;  // Prevent accidental double-switches
    [SerializeField] private float scrollSensitivity = 0.05f;     // Threshold for scroll detection

    private int currentWeaponIndex = 0;
    private PlayerWeapon[] weapons;
    private float lastSwitchTime = 0f;

    void Start()
    {
        weapons = new PlayerWeapon[] { primaryWeapon, secondaryWeapon };

        // Enable primary weapon by default
        if (primaryWeapon != null)
            primaryWeapon.gameObject.SetActive(true);
        if (secondaryWeapon != null)
            secondaryWeapon.gameObject.SetActive(false);

        // Show initial weapon
        if (weaponIdentifier != null && primaryWeapon != null)
        {
            weaponIdentifier.ShowWeapon(primaryWeapon);
        }
    }

    void Update()
    {
        // Respect cooldown for smoother switching
        if (Time.time - lastSwitchTime < weaponSwitchCooldown)
            return;

        // Weapon switching with number keys (instant, no cooldown check needed)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1);
        }

        // Mouse scroll weapon switching with sensitivity threshold
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > scrollSensitivity)
        {
            if (scroll > 0f)
            {
                SwitchWeapon((currentWeaponIndex + 1) % weapons.Length);
            }
            else if (scroll < 0f)
            {
                SwitchWeapon((currentWeaponIndex - 1 + weapons.Length) % weapons.Length);
            }
        }
    }

    void SwitchWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Length || weapons[weaponIndex] == null)
            return;

        if (weaponIndex == currentWeaponIndex)
            return;

        // Disable current weapon
        if (weapons[currentWeaponIndex] != null)
            weapons[currentWeaponIndex].gameObject.SetActive(false);

        // Enable new weapon
        currentWeaponIndex = weaponIndex;
        weapons[currentWeaponIndex].gameObject.SetActive(true);

        // Update switch time for cooldown
        lastSwitchTime = Time.time;

        // Show weapon identifier
        if (weaponIdentifier != null)
        {
            weaponIdentifier.ShowWeapon(weapons[currentWeaponIndex]);
        }
    }

    public void Fire()
    {
        if (weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].Fire();
        }
    }

    public PlayerWeapon GetCurrentWeapon()
    {
        return weapons[currentWeaponIndex];
    }
}
