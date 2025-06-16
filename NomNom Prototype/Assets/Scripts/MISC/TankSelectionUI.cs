using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TankSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class TankButtonEntry
    {
        public Button button;
        public Image backgroundImage; // Used for custom color highlighting
    }

    [SerializeField] private List<TankButtonEntry> tankButtons;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private int selectedIndex = -1;

    private void Start()
    {
        for (int i = 0; i < tankButtons.Count; i++)
        {
            int index = i; // Capture local for closure
            tankButtons[i].button.onClick.AddListener(() => SelectTank(index));
        }

        // Optionally, auto-select default tank (index 0)
        SelectTank(0);
    }

    private void SelectTank(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < tankButtons.Count; i++)
        {
            if (tankButtons[i].backgroundImage != null)
            {
                tankButtons[i].backgroundImage.color = (i == index) ? selectedColor : normalColor;
            }
        }

        var networkController = FindObjectOfType<NetworkTankController>();
        if (networkController != null && networkController.IsOwner)
        {
            networkController.SubmitTankChoiceServerRpc(index);
            Debug.Log($"[TankSelectionUI] Selected tank index: {index}");
        }
    }
}
