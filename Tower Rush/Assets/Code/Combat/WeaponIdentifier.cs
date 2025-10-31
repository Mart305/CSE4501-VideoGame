using UnityEngine;
using UnityEngine.UI;

public class WeaponIdentifier : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text weaponNameText;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private float displayDuration = 2f;

    private PlayerWeapon currentWeapon;
    private float displayTimer = 0f;
    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, displayTimer / displayDuration);
        }
        else
        {
            canvasGroup.alpha = 0f;
        }
    }

    public void ShowWeapon(PlayerWeapon weapon)
    {
        currentWeapon = weapon;
        displayTimer = displayDuration;

        if (weaponNameText != null)
        {
            weaponNameText.text = GetWeaponDisplayName(weapon);
            weaponNameText.color = GetWeaponColor(weapon);
        }

        if (weaponIcon != null)
        {
            weaponIcon.color = GetWeaponColor(weapon);
        }
    }

    string GetWeaponDisplayName(PlayerWeapon weapon)
    {
        return weapon.GetWeaponName();
    }

    Color GetWeaponColor(PlayerWeapon weapon)
    {
        WeaponType wType = weapon.GetWeaponType();

        switch (wType)
        {
            case WeaponType.Pistol:
                return new Color(0.9f, 0.9f, 0.5f);
            case WeaponType.Rifle:
                return new Color(1f, 0.6f, 0.2f);
            case WeaponType.Shotgun:
                return new Color(1f, 0.4f, 0f);
            case WeaponType.Sniper:
                return new Color(0.7f, 0.8f, 1f);
            default:
                return Color.white;
        }
    }
}
