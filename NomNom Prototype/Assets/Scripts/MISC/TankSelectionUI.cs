using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TankSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class TankButtonEntry
    {
        public Button button;                // The actual UI Button
        public Image backgroundImage;        // The background that changes color when selected
    }

    [Header("Tank Button Entries")]
    [SerializeField] private List<TankButtonEntry> tankButtons;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private int selectedIndex = -1;

    private void Start()
    {
        // Hook up button listeners
        for (int i = 0; i < tankButtons.Count; i++)
        {
            int index = i;
            tankButtons[i].button.onClick.AddListener(() => SelectTank(index));
        }

        // Set default selection focus for controller users
        if (tankButtons.Count > 0 && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(tankButtons[0].button.gameObject);
        }

        // Optionally preselect the first tank
        SelectTank(0);
    }

    private void Update()
    {
        // Handle controller "Submit" (e.g., A button or Enter key)
        if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return))
        {
            GameObject selectedGO = EventSystem.current?.currentSelectedGameObject;

            for (int i = 0; i < tankButtons.Count; i++)
            {
                if (tankButtons[i].button.gameObject == selectedGO)
                {
                    SelectTank(i);
                    break;
                }
            }
        }
    }

    private void SelectTank(int index)
    {
        selectedIndex = index;

        // Update background highlights
        for (int i = 0; i < tankButtons.Count; i++)
        {
            if (tankButtons[i].backgroundImage != null)
            {
                tankButtons[i].backgroundImage.color = (i == index) ? selectedColor : normalColor;
            }
        }

        // Notify server via NetworkTankController
        var networkController = FindObjectOfType<NetworkTankController>();
        if (networkController != null && networkController.IsOwner)
        {
            networkController.SubmitTankChoiceServerRpc(index);
            Debug.Log($"[TankSelectionUI] Selected tank index: {index}");
        }
    }
}
