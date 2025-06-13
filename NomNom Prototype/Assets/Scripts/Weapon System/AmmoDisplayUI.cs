using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplayUI : MonoBehaviour
{
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f;

    private ProjectileFactory projectileFactory;
    private float updateTimer = 0f;

    [Header("Ammo Bar Images")]
    [SerializeField] private Image[] simpleAmmoBars;
    [SerializeField] private Image[] homingAmmoBars;
    [SerializeField] private Image[] arcAmmoBars;

    [Header("Simple Weapon Colors")]
    [SerializeField] private Color simpleFullColor = Color.green;
    [SerializeField] private Color simpleReloadColor = Color.yellow;
    [SerializeField] private Color simpleEmptyColor = Color.red;

    [Header("Homing Weapon Colors")]
    [SerializeField] private Color homingFullColor = Color.cyan;
    [SerializeField] private Color homingReloadColor = Color.blue;
    [SerializeField] private Color homingEmptyColor = Color.gray;

    [Header("Arc Weapon Colors")]
    [SerializeField] private Color arcFullColor = new Color(1f, 0.5f, 0f); // orange
    [SerializeField] private Color arcReloadColor = Color.yellow;
    [SerializeField] private Color arcEmptyColor = Color.gray;

    private void OnEnable()
    {
        Debug.Log("[AmmoDisplayUI] OnEnable() called.");

        // 🚀 Always subscribe → ensures we track new tank after respawn
        GameManager.OnLocalPlayerFactoryAssigned += OnLocalPlayerFactoryReady;

        // 🚀 If already assigned → assign now
        if (GameManager.LocalPlayerFactory != null)
        {
            OnLocalPlayerFactoryReady();
        }
    }

    private void OnDisable()
    {
        Debug.Log("[AmmoDisplayUI] OnDisable() → Unsubscribing from OnLocalPlayerFactoryAssigned.");
        GameManager.OnLocalPlayerFactoryAssigned -= OnLocalPlayerFactoryReady;
    }

    private void OnLocalPlayerFactoryReady()
    {
        projectileFactory = GameManager.LocalPlayerFactory;
        Debug.Log("[AmmoDisplayUI] Found LocalPlayerFactory → AmmoDisplayUI active (via callback).");
    }

    private void Update()
    {
        if (projectileFactory == null) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateAmmoUI();
            updateTimer = 0f;
        }
    }

    private void UpdateAmmoUI()
    {
        var simpleAmmo = projectileFactory.GetSimpleAmmoInfo();
        var homingAmmo = projectileFactory.GetHomingAmmoInfo();
        var arcAmmo = projectileFactory.GetArcAmmoInfo();

        UpdateWeaponAmmoBars(simpleAmmo, simpleAmmoBars, simpleFullColor, simpleReloadColor, simpleEmptyColor);
        UpdateWeaponAmmoBars(homingAmmo, homingAmmoBars, homingFullColor, homingReloadColor, homingEmptyColor);
        UpdateWeaponAmmoBars(arcAmmo, arcAmmoBars, arcFullColor, arcReloadColor, arcEmptyColor);
    }

    private void UpdateWeaponAmmoBars(
        ProjectileFactory.AmmoInfo ammoInfo,
        Image[] ammoBars,
        Color fullColor,
        Color reloadColor,
        Color emptyColor)
    {
        for (int i = 0; i < ammoBars.Length; i++)
        {
            if (ammoBars[i] == null) continue;

            if (i < ammoInfo.currentAmmo)
            {
                // Full
                ammoBars[i].fillAmount = 1f;
                ammoBars[i].color = fullColor;
            }
            else if (i == ammoInfo.currentAmmo && ammoInfo.currentAmmo < ammoInfo.maxAmmo)
            {
                // Reloading this bullet
                ammoBars[i].fillAmount = ammoInfo.reloadProgress;
                ammoBars[i].color = Color.Lerp(emptyColor, reloadColor, ammoInfo.reloadProgress);
            }
            else
            {
                // Empty
                ammoBars[i].fillAmount = 0f;
                ammoBars[i].color = emptyColor;
            }
        }
    }
}
