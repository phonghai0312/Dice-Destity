using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class FixCombatUI
{
    [MenuItem("Tools/Fix Combat UI")]
    public static void DoFix()
    {
        if (EditorApplication.isPlaying) {
            Debug.LogError("Hãy tắt Play mode trước khi chạy Setup!");
            return;
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");

        // Hide the white placeholder background of Player and Enemy areas
        GameObject pArea = GameObject.Find("PlayerArea");
        if (pArea != null) {
            Image pImg = pArea.GetComponent<Image>();
            if (pImg != null) pImg.enabled = false;
        }

        GameObject eArea = GameObject.Find("EnemyArea");
        if (eArea != null) {
            Image eImg = eArea.GetComponent<Image>();
            if (eImg != null) eImg.enabled = false;
        }

        // Remove extra KILL buttons
        GameObject aPanel = GameObject.Find("ActionPanel");
        if (aPanel != null) {
            int killCount = 0;
            for (int i = aPanel.transform.childCount - 1; i >= 0; i--) {
                Transform child = aPanel.transform.GetChild(i);
                if (child.name.Contains("DebugKillBtn") || child.name.Contains("KILL")) {
                    killCount++;
                    if (killCount > 1) { // Keep only one
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Combat UI Cleaned!");
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }
}
