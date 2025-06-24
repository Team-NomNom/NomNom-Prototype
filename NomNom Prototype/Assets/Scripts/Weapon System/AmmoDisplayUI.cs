using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AmmoDisplayUI : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject weaponSlotContainerPrefab;
    [SerializeField] private GameObject barImagePrefab;

    [Header("Parent Container")]
    [SerializeField] private Transform weaponSlotsParentContainer; // Vertical Layout Group

    private ProjectileFactory projectileFactory;
    private TeleportBeaconController teleportController;

    private List<List<Image>> barImagesPerSlot = new();

    private void OnEnable()
    {
        GameManager.OnLocalPlayerFactoryAssigned += HandleLocalPlayerFactoryAssigned;
    }

    private void OnDisable()
    {
        GameManager.OnLocalPlayerFactoryAssigned -= HandleLocalPlayerFactoryAssigned;
    }

    private void HandleLocalPlayerFactoryAssigned()
    {
        SetProjectileFactory(GameManager.LocalPlayerFactory);

        var factoryGO = GameManager.LocalPlayerFactory?.gameObject;
        if (factoryGO != null)
        {
            teleportController = factoryGO.GetComponentInParent<TeleportBeaconController>();
        }
    }

    public void SetProjectileFactory(ProjectileFactory factory)
    {
        projectileFactory = factory;
        RebuildWeaponSlots();
    }

    private void RebuildWeaponSlots()
    {
        foreach (Transform child in weaponSlotsParentContainer)
            Destroy(child.gameObject);

        barImagesPerSlot.Clear();

        if (projectileFactory == null) return;

        for (int i = 0; i < projectileFactory.weaponSlots.Count; i++)
        {
            var slot = projectileFactory.weaponSlots[i];

            GameObject slotGO = Instantiate(weaponSlotContainerPrefab, weaponSlotsParentContainer);
            slotGO.name = $"WeaponSlot{i}_Container";

            List<Image> bars = new();
            int barCount = slot.IsTeleportBeacon ? 1 : slot.ammoSettings.maxAmmo;

            for (int j = 0; j < barCount; j++)
            {
                GameObject barObj = Instantiate(barImagePrefab, slotGO.transform);
                Image barImg = barObj.GetComponent<Image>();
                if (barImg == null) continue;

                barImg.fillAmount = 1.0f;
                bars.Add(barImg);
            }

            barImagesPerSlot.Add(bars);
        }
    }

    private void Update()
    {
        if (projectileFactory == null) return;

        for (int i = 0; i < projectileFactory.weaponSlots.Count; i++)
        {
            if (i >= barImagesPerSlot.Count) continue;

            var slot = projectileFactory.weaponSlots[i];
            var bars = barImagesPerSlot[i];

            if (slot.IsTeleportBeacon && teleportController != null)
            {
                float fill = 1f - Mathf.Clamp01(teleportController.CooldownRemaining / teleportController.CooldownDuration);
                bars[0].fillAmount = fill;
            }
            else
            {
                var info = projectileFactory.GetAmmoInfo(i);
                for (int j = 0; j < bars.Count; j++)
                {
                    float targetFill = 0f;
                    if (j < info.currentAmmo) targetFill = 1f;
                    else if (j == info.currentAmmo) targetFill = info.reloadProgress;
                    bars[j].fillAmount = targetFill;
                }
            }
        }
    }
}
