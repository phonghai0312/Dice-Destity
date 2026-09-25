using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Events;

public class Phase2Setup
{
    [MenuItem("Tools/Setup Phase 2")]
    public static void DoSetup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (GameObject.Find("EventSystem") == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModuleType != null) {
                esObj.AddComponent(newModuleType);
            } else {
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds 1/bg_dugeon_state_1.jpg");
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject logoObj = new GameObject("Logo");
        logoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform logoRt = logoObj.AddComponent<RectTransform>();
        logoRt.anchoredPosition = new Vector2(0, 150);
        logoRt.sizeDelta = new Vector2(800, 400);
        Image logoImg = logoObj.AddComponent<Image>();
        logoImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI 1/DICE & DESTINY.png");
        logoImg.preserveAspect = true;

        GameObject CreateButton(string name, string text, float yPos)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, yPos);
            rt.sizeDelta = new Vector2(300, 80);
            Image img = btnObj.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI 1/red_button.png");
            Button btn = btnObj.AddComponent<Button>();

            System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null) {
                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
                Component tmp = txtObj.AddComponent(tmpType);
                tmpType.GetProperty("text").SetValue(tmp, text);
                tmpType.GetProperty("alignment").SetValue(tmp, 514);
                tmpType.GetProperty("fontSize").SetValue(tmp, 36f);
                tmpType.GetProperty("color").SetValue(tmp, Color.black);
            }
            return btnObj;
        }

        GameObject startBtn = CreateButton("StartRunButton", "START RUN", -100);
        GameObject settingsBtn = CreateButton("SettingsButton", "SETTINGS", -200);
        GameObject quitBtn = CreateButton("QuitButton", "QUIT", -300);

        GameObject controllerObj = new GameObject("MainMenuController");
        var mmc = controllerObj.AddComponent<MainMenuController>();
        
        UnityEventTools.AddPersistentListener(startBtn.GetComponent<Button>().onClick, mmc.OnStartRunClicked);
        UnityEventTools.AddPersistentListener(settingsBtn.GetComponent<Button>().onClick, mmc.OnSettingsClicked);
        UnityEventTools.AddPersistentListener(quitBtn.GetComponent<Button>().onClick, mmc.OnQuitClicked);

        if (GameObject.Find("GameManager") == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("PHASE 2 SETUP COMPLETE");
    }
}
