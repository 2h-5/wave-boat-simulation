using UnityEngine;

public class InstructionWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject instructionPanel;

    public void CloseInstructionWindow()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        else
        {
            // If no panel was assigned, hide this GameObject instead.
            gameObject.SetActive(false);
        }
    }

    public void OpenInstructionWindow()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
    }
}