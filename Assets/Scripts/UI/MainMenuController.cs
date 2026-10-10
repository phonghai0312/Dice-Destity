using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenuController — Tự tìm nút Start và wire up khi scene load.
/// Không cần assign thủ công.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private void Start()
    {
        // Tự tìm nút StartRunButton và wire up
        GameObject startGo = GameObject.Find("StartRunButton");
        if (startGo != null)
        {
            Button btn = startGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnStartRunClicked);
            }
        }

        // Tự tìm CONTINUE và CODEX (hiện tại disable vì chưa có feature)
        GameObject contGo = GameObject.Find("ContinueButton");
        if (contGo != null) contGo.GetComponent<Button>()?.onClick.AddListener(() => Debug.Log("Continue: Coming soon!"));

        GameObject codexGo = GameObject.Find("CodexButton");
        if (codexGo != null) codexGo.GetComponent<Button>()?.onClick.AddListener(() => Debug.Log("Codex: Coming soon!"));
    }

    public void OnStartRunClicked()
    {
        Debug.Log("[MainMenu] Starting New Run...");

        // Tạo GameManager nếu chưa có
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewRun();
        }
        else
        {
            // Fallback cứng
            Debug.LogError("[MainMenu] GameManager still null! Loading CombatScene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("CombatScene");
        }
    }

    public void OnSettingsClicked() => Debug.Log("Settings: Coming soon!");
    public void OnQuitClicked() => Application.Quit();
}
