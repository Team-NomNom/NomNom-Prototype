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

    // Internal tracking
    private List<List<Image>> barImagesPerSlot = new List<List<Image>>();

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
    }

    public void SetProjectileFactory(ProjectileFactory factory)
    {
        projectileFactory = factory;
        RebuildWeaponSlots();
    }

    private void RebuildWeaponSlots()
    {
        // Clear existing weapon slot containers
        foreach (Transform child in weaponSlotsParentContainer)
        {
            Destroy(child.gameObject);
        }

        barImagesPerSlot.Clear();

        if (projectileFactory == null) return;

        // For each weapon slot
        for (int weaponSlotIndex = 0; weaponSlotIndex < projectileFactory.weaponSlots.Count; weaponSlotIndex++)
        {
            var weaponSlot = projectileFactory.weaponSlots[weaponSlotIndex];

            // Instantiate WeaponSlotContainer
            GameObject slotContainerGO = Instantiate(weaponSlotContainerPrefab, weaponSlotsParentContainer);
            slotContainerGO.name = $"WeaponSlot{weaponSlotIndex}_Container";

            // List to track bar images for this slot
            List<Image> barImages = new List<Image>();

            int maxAmmo = weaponSlot.ammoSettings.maxAmmo;

            // Instantiate BarImages
            for (int i = 0; i < maxAmmo; i++)
            {
                GameObject barObj = Instantiate(barImagePrefab, slotContainerGO.transform);
                Image barImage = barObj.GetComponent<Image>();

                if (barImage == null)
                {
                    Debug.LogError("[AmmoDisplayUI] BarImagePrefab is missing an Image component!");
                    continue;
                }

                barImage.fillAmount = 1.0f; // full by default
                barImages.Add(barImage);
            }

            barImagesPerSlot.Add(barImages);
        }
    }

    private void Update()
    {
        if (projectileFactory == null)
        {
            Debug.Log("[AmmoDisplayUI] Update → projectileFactory is null → skipping.");
            return;
        }

        // Debug.Log("[AmmoDisplayUI] Update running.");

        for (int slotIndex = 0; slotIndex < projectileFactory.weaponSlots.Count; slotIndex++)
        {
            if (slotIndex >= barImagesPerSlot.Count)
            {
                Debug.LogWarning($"[AmmoDisplayUI] SlotIndex {slotIndex} has no corresponding BarImages list → skipping.");
                continue;
            }

            var ammoInfo = projectileFactory.GetAmmoInfo(slotIndex);

            // Debug.Log($"[AmmoDisplayUI] Slot {slotIndex} → currentAmmo={ammoInfo.currentAmmo}, maxAmmo={ammoInfo.maxAmmo}, reloadProgress={ammoInfo.reloadProgress}");

            var barImages = barImagesPerSlot[slotIndex];

            for (int i = 0; i < barImages.Count; i++)
            {
                float targetFill = 0f;

                if (i < ammoInfo.currentAmmo)
                {
                    targetFill = 1.0f; // Full ammo bar
                }
                else if (i == ammoInfo.currentAmmo)
                {
                    targetFill = ammoInfo.reloadProgress; // Currently reloading bar
                }
                else
                {
                    targetFill = 0.0f; // Empty bar
                }

                // Log per-bar fill (optional → can comment out if spammy)
                // Debug.Log($"[AmmoDisplayUI] Slot {slotIndex} → Bar {i} → targetFill={targetFill}");

                barImages[i].fillAmount = targetFill;
            }
        }
    }

}
