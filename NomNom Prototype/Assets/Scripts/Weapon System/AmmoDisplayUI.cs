using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Text simpleAmmoText;
    [SerializeField] private Text homingAmmoText;
    [SerializeField] private Text arcAmmoText;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f;

    private ProjectileFactory projectileFactory;
    private float updateTimer = 0f;

    private void OnEnable()
    {
        Debug.Log("[AmmoDisplayUI] OnEnable() called.");

        if (GameManager.LocalPlayerFactory != null)
        {
            projectileFactory = GameManager.LocalPlayerFactory;
            Debug.Log("[AmmoDisplayUI] Found LocalPlayerFactory → AmmoDisplayUI active (immediate).");
        }
        else
        {
            GameManager.OnLocalPlayerFactoryAssigned += OnLocalPlayerFactoryReady;
            Debug.Log("[AmmoDisplayUI] Waiting for LocalPlayerFactory assignment...");
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

        GameManager.OnLocalPlayerFactoryAssigned -= OnLocalPlayerFactoryReady;
    }

    private void Update()
    {
        if (projectileFactory == null)
        {
            // In case the tank was destroyed and LocalPlayerFactory was reassigned:
            if (GameManager.LocalPlayerFactory != null && GameManager.LocalPlayerFactory != projectileFactory)
            {
                projectileFactory = GameManager.LocalPlayerFactory;
                Debug.Log("[AmmoDisplayUI] Re-acquired LocalPlayerFactory (Update fallback).");
            }
            else
            {
                return;
            }
        }

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

        if (simpleAmmoText != null)
            simpleAmmoText.text = $"Simple: {simpleAmmo.currentAmmo} / {simpleAmmo.maxAmmo}";

        if (homingAmmoText != null)
            homingAmmoText.text = $"Homing: {homingAmmo.currentAmmo} / {homingAmmo.maxAmmo}";

        if (arcAmmoText != null)
            arcAmmoText.text = $"Arc: {arcAmmo.currentAmmo} / {arcAmmo.maxAmmo}";
    }
}
