using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
        // 1. Tự tìm nút StartRunButton / NewGameButton và wire up
        GameObject startGo = FindFirstButtonObject("StartRunButton", "NewGameButton");
        if (startGo != null)
        {
            Button btn = startGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNewGameClicked);
            }
        }

        // 2. Tự tìm CONTINUE
        GameObject contGo = FindFirstButtonObject("ContinueButton");
        if (contGo != null)
        {
            Button btn = contGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnContinueClicked);
            }
        }

        // 3. Tự tìm SETTINGS (trong MainMenu.unity nút mang sprite UI_Button_Setting đang đặt tên là CodexButton)
        GameObject settingGo = FindFirstButtonObject("SettingButton", "SettingsButton", "CodexButton");
        if (settingGo != null)
        {
            Button btn = settingGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnSettingClicked);
            }
        }

        // 4. Tự tìm QUIT / EXIT (trong MainMenu.unity đặt tên là Exitbutton chữ b thường)
        GameObject exitGo = FindFirstButtonObject("ExitButton", "Exitbutton", "QuitButton");
        if (exitGo != null)
        {
            Button btn = exitGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnExitClicked);
            }
        }
    }

    private static GameObject FindFirstButtonObject(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            GameObject found = GameObject.Find(names[i]);
            if (found != null)
            {
                return found;
            }
        }
        return null;
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
        GameManager gameManager = GameManager.ResolveInstance();
        if (gameManager != null && gameManager.playerCurrentHP > 0 && gameManager.currentStageIndex > 0)
        {
            Debug.Log($"[MainMenu] Continuing current run at Stage {gameManager.currentStageIndex + 1} (HP: {gameManager.playerCurrentHP}/{gameManager.playerMaxHP})...");
            SceneManager.LoadScene("CombatScene");
        }
        else if (gameManager != null)
        {
            Debug.Log("[MainMenu] Starting / Resuming Run at Stage 1...");
            gameManager.StartNewRun();
        }
    }

    // 3. Settings
    public void OnSettingClicked()
    {
        PlayClickSound();
        Debug.Log("[MainMenu] Settings button clicked!");
        if (settingsPanel == null)
        {
            settingsPanel = BuildRuntimeSettingsPanel();
        }
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

    private GameObject BuildRuntimeSettingsPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        GameObject modalObj = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image));
        modalObj.transform.SetParent(canvas.transform, false);
        modalObj.transform.SetAsLastSibling();

        RectTransform modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        Image backdropImg = modalObj.GetComponent<Image>();
        backdropImg.color = new Color(0.02f, 0.01f, 0.03f, 0.82f);

        GameObject boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        boxObj.transform.SetParent(modalObj.transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(520f, 340f);
        boxRect.anchoredPosition = Vector2.zero;

        Image boxImg = boxObj.GetComponent<Image>();
        boxImg.color = new Color(0.11f, 0.08f, 0.14f, 0.98f);

        Outline outline = boxObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.68f, 0.28f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(boxObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(460f, 60f);
        titleRect.anchoredPosition = new Vector2(0f, -45f);

        TextMeshProUGUI titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "SETTINGS";
        titleTmp.fontSize = 32f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(0.95f, 0.78f, 0.35f, 1f);

        // Info text
        GameObject infoObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoObj.transform.SetParent(boxObj.transform, false);
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.5f, 0.5f);
        infoRect.anchorMax = new Vector2(0.5f, 0.5f);
        infoRect.sizeDelta = new Vector2(440f, 120f);
        infoRect.anchoredPosition = new Vector2(0f, 10f);

        TextMeshProUGUI infoTmp = infoObj.GetComponent<TextMeshProUGUI>();
        infoTmp.text = "DICE & DESTINY — Dark Fantasy Roguelike\n\n• Drag rolled dice onto Hero (Shield) or Enemy (Attack/Poison)\n• Upgrade your 6-sided die in The Forge after each victory";
        infoTmp.fontSize = 18f;
        infoTmp.alignment = TextAlignmentOptions.Center;
        infoTmp.color = new Color(0.88f, 0.85f, 0.8f, 1f);

        // Close Button
        GameObject closeBtnObj = new GameObject("CloseSettingsButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(boxObj.transform, false);
        RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.sizeDelta = new Vector2(200f, 52f);
        closeRect.anchoredPosition = new Vector2(0f, 45f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.55f, 0.16f, 0.16f, 1f);

        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.onClick.AddListener(() => modalObj.SetActive(false));

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = Vector2.zero;
        closeTxtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeTmp = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTmp.text = "CLOSE";
        closeTmp.fontSize = 20f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.white;

        modalObj.SetActive(false);
        return modalObj;
    }
}
