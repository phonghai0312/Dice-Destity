using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public void OnStartRunClicked()
    {
        Debug.Log("Starting New Run...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewRun();
        }
        else
        {
            Debug.LogError("GameManager is missing!");
        }
    }

    public void OnSettingsClicked()
    {
        Debug.Log("Settings button clicked!");
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quit game.");
        Application.Quit();
    }
}
