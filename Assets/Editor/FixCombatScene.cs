using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixCombatScene
{
    [MenuItem("Tools/Fix Combat Scene")]
    public static void DoFix()
    {
        if (EditorApplication.isPlaying) {
            Debug.LogError("Hãy tắt Play mode trước khi chạy Setup!");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");

        // Bật CombatPanel
        GameObject combatPanel = GameObject.Find("CombatPanel");
        if (combatPanel == null) 
        {
            // Try to find inactive ones
            Transform canvas = GameObject.Find("MainCanvas").transform;
            if (canvas != null)
            {
                Transform cp = canvas.Find("CombatPanel");
                if (cp != null) combatPanel = cp.gameObject;
            }
        }
        
        if (combatPanel != null)
        {
            combatPanel.SetActive(true);
        }

        // Xoá MainMenuPanel và ForgePanel thừa trong CombatScene
        Transform mainCanvas = GameObject.Find("MainCanvas").transform;
        if (mainCanvas != null)
        {
            Transform mm = mainCanvas.Find("MainMenuPanel");
            if (mm != null) Object.DestroyImmediate(mm.gameObject);

            Transform fp = mainCanvas.Find("ForgePanel");
            if (fp != null) Object.DestroyImmediate(fp.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("Đã dọn dẹp CombatScene thành công! Giờ nó chỉ chứa UI của Combat.");
        
        // Trở về MainMenu
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }
}
