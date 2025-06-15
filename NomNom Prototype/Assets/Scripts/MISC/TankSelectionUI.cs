using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TankSelectionUI : MonoBehaviour
{
    [SerializeField] private List<Button> tankButtons;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    private int selectedIndex = -1;

    private void Start()
    {
        for (int i = 0; i < tankButtons.Count; i++)
        {
            int index = i; // Capture local for closure
            tankButtons[i].onClick.AddListener(() => SelectTank(index));
        }
    }

    private void SelectTank(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < tankButtons.Count; i++)
        {
            var colors = tankButtons[i].colors;
            colors.normalColor = (i == index) ? selectedColor : normalColor;
            tankButtons[i].colors = colors;
        }

        var networkController = FindObjectOfType<NetworkTankController>();
        if (networkController != null && networkController.IsOwner)
        {
            networkController.SubmitTankChoiceServerRpc(index);
            Debug.Log($"[TankSelectionUI] Selected tank index: {index}");
        }
    }
}
