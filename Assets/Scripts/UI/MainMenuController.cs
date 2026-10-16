using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Panels / Modals (Optional)")]
    [SerializeField] private GameObject settingsPanel;

    private void PlayClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    // 1. New Game
    public void OnNewGameClicked()
    {
        PlayClickSound();
        Debug.Log("Starting New Game / Run...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewRun();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance not found! Attempting direct scene load or fallback...");
        }
    }

    // Backward compatibility for existing OnStartRunClicked hookup
    public void OnStartRunClicked() => OnNewGameClicked();

    // 2. Continue
    public void OnContinueClicked()
    {
        PlayClickSound();
        Debug.Log("Continue previous save...");
        // Add save game load logic when ready
    }

    // 3. Settings
    public void OnSettingClicked()
    {
        PlayClickSound();
        Debug.Log("Settings button clicked!");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    // Backward compatibility for existing OnSettingsClicked
    public void OnSettingsClicked() => OnSettingClicked();

    // 4. Exit
    public void OnExitClicked()
    {
        PlayClickSound();
        Debug.Log("Quit game requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Backward compatibility for existing OnQuitClicked
    public void OnQuitClicked() => OnExitClicked();
}
