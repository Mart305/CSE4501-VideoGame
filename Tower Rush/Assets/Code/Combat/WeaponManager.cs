using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private PlayerWeapon primaryWeapon;
    [SerializeField] private ShotgunWeapon secondaryWeapon;

    private int currentWeaponIndex = 0;
    private PlayerWeapon[] weapons;

    void Start()
    {
        weapons = new PlayerWeapon[] { primaryWeapon, secondaryWeapon };

        // Enable primary weapon by default
        if (primaryWeapon != null)
            primaryWeapon.gameObject.SetActive(true);
        if (secondaryWeapon != null)
            secondaryWeapon.gameObject.SetActive(false);
    }

    void Update()
    {
        // Weapon switching with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1);
        }

        // Mouse scroll weapon switching
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            SwitchWeapon((currentWeaponIndex + 1) % weapons.Length);
        }
        else if (scroll < 0f)
        {
            SwitchWeapon((currentWeaponIndex - 1 + weapons.Length) % weapons.Length);
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
