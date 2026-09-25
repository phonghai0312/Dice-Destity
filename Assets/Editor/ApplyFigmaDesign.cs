using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ApplyFigmaDesign
{
    [MenuItem("Tools/Apply Figma Design")]
    public static void DoSetup()
    {
        if (EditorApplication.isPlaying) {
            Debug.LogError("Hãy tắt Play mode trước khi chạy Setup!");
            return;
        }

        Color colBg, colCard, colPrimary, colPrimaryFg, colAccent, colAccentFg;
        ColorUtility.TryParseHtmlString("#0a090d", out colBg);
        ColorUtility.TryParseHtmlString("#13111a", out colCard);
        ColorUtility.TryParseHtmlString("#a81c1c", out colPrimary);
        ColorUtility.TryParseHtmlString("#fef2f2", out colPrimaryFg);
        ColorUtility.TryParseHtmlString("#b8860b", out colAccent);
        ColorUtility.TryParseHtmlString("#fdf6e3", out colAccentFg);

        System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        void StyleButton(GameObject btn, Color bgColor, Color txtColor) {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = bgColor;
            if (tmpType != null && btn.transform.childCount > 0) {
                Component txt = btn.transform.GetChild(0).GetComponent(tmpType);
                if (txt != null) {
                    var prop = tmpType.GetProperty("color");
                    if (prop != null) prop.SetValue(txt, txtColor);
                }
            }
        }

        // 1. MAIN MENU
        Scene menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        StyleButton(GameObject.Find("StartRunButton"), colPrimary, colPrimaryFg);
        StyleButton(GameObject.Find("SettingsButton"), colCard, colAccentFg);
        StyleButton(GameObject.Find("QuitButton"), colCard, colAccentFg);
        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene);

        // 2. COMBAT SCENE
        Scene combatScene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");
        GameObject pArea = GameObject.Find("PlayerArea");
        if (pArea) {
            Image img = pArea.GetComponent<Image>();
            if (img) { img.enabled = true; img.color = new Color(colCard.r, colCard.g, colCard.b, 0.85f); }
        }
        GameObject eArea = GameObject.Find("EnemyArea");
        if (eArea) {
            Image img = eArea.GetComponent<Image>();
            if (img) { img.enabled = true; img.color = new Color(colCard.r, colCard.g, colCard.b, 0.85f); }
        }
        
        GameObject actionPanel = GameObject.Find("ActionPanel");
        if (actionPanel) {
            Button[] btns = actionPanel.GetComponentsInChildren<Button>();
            foreach (var b in btns) {
                Image img = b.GetComponent<Image>();
                if (img != null) {
                    if (b.name.Contains("End")) img.color = colAccent;
                    else if (b.name.Contains("KILL")) img.color = colPrimary;
                    else img.color = colCard;
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(combatScene);
        EditorSceneManager.SaveScene(combatScene);

        // 3. THE FORGE
        Scene forgeScene = EditorSceneManager.OpenScene("Assets/Scenes/TheForge.unity");
        StyleButton(GameObject.Find("NextStageButton"), colPrimary, colPrimaryFg);
        EditorSceneManager.MarkSceneDirty(forgeScene);
        EditorSceneManager.SaveScene(forgeScene);

        Debug.Log("Figma Colors Applied to all scenes!");
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }
}
