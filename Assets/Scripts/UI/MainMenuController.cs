using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenuController — Tự tìm nút và wire up khi scene load, đồng thời cung cấp các handler cho cả 4 nút UI.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Panels / Modals (Optional)")]
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        // Tự tìm nút StartRunButton / NewGameButton và wire up
        GameObject startGo = GameObject.Find("StartRunButton") ?? GameObject.Find("NewGameButton");
        if (startGo != null)
        {
            Button btn = startGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNewGameClicked);
            }
        }

        // Tự tìm CONTINUE
        GameObject contGo = GameObject.Find("ContinueButton");
        if (contGo != null)
        {
            Button btn = contGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnContinueClicked);
            }
        }

        // Tự tìm SETTINGS
        GameObject settingGo = GameObject.Find("SettingButton") ?? GameObject.Find("SettingsButton");
        if (settingGo != null)
        {
            Button btn = settingGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnSettingClicked);
            }
        }

        // Tự tìm QUIT / EXIT
        GameObject exitGo = GameObject.Find("ExitButton") ?? GameObject.Find("QuitButton");
        if (exitGo != null)
        {
            Button btn = exitGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnExitClicked);
            }
        }

        GameObject codexGo = GameObject.Find("CodexButton");
        if (codexGo != null) codexGo.GetComponent<Button>()?.onClick.AddListener(() => Debug.Log("Codex: Coming soon!"));
    }

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
        Debug.Log("[MainMenu] Starting New Game / Run...");

        // Prefer the persistent singleton, but recover a configured manager from
        // the active MainMenu scene if its Awake did not register it.
        GameManager gameManager = GameManager.ResolveInstance();

        if (gameManager == null)
        {
            Debug.LogError("[MainMenu] No GameManager found in the active scene. Add the configured GameManager from MainMenu.unity.");
            return;
        }

        gameManager.StartNewRun();
    }

    // Backward compatibility for existing OnStartRunClicked hookup
    public void OnStartRunClicked() => OnNewGameClicked();

    // 2. Continue
    public void OnContinueClicked()
    {
        PlayClickSound();
        Debug.Log("[MainMenu] Continue previous save...");
    }

    // 3. Settings
    public void OnSettingClicked()
    {
        PlayClickSound();
        Debug.Log("[MainMenu] Settings button clicked!");
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
        Debug.Log("[MainMenu] Quit game requested.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Backward compatibility for existing OnQuitClicked
    public void OnQuitClicked() => OnExitClicked();
}
