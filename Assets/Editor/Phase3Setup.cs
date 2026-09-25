using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor.Events;
using System.Collections.Generic;

public class Phase3Setup
{
    [MenuItem("Tools/Setup Phase 3")]
    public static void DoSetup()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TheForge.unity");

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
            if (newModuleType != null) esObj.AddComponent(newModuleType);
            else esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        RectTransform bgRt = bgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Title
        GameObject logoObj = new GameObject("Title");
        logoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform logoRt = logoObj.AddComponent<RectTransform>();
        logoRt.anchoredPosition = new Vector2(0, 300);
        logoRt.sizeDelta = new Vector2(600, 100);
        Image logoImg = logoObj.AddComponent<Image>();
        logoImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI 1/Roll your fate — Forge your legend.png");
        logoImg.preserveAspect = true;

        // Reward Container
        GameObject rContainer = new GameObject("RewardsContainer");
        rContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform rRt = rContainer.AddComponent<RectTransform>();
        rRt.anchoredPosition = new Vector2(0, 100);
        rRt.sizeDelta = new Vector2(600, 150);
        HorizontalLayoutGroup rLayout = rContainer.AddComponent<HorizontalLayoutGroup>();
        rLayout.childAlignment = TextAnchor.MiddleCenter;
        rLayout.spacing = 50;
        
        for (int i = 0; i < 3; i++)
        {
            GameObject face = new GameObject("Reward_" + i);
            face.transform.SetParent(rContainer.transform, false);
            face.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            face.AddComponent<Image>();
            face.AddComponent<Button>();
        }

        // Current Dice Container
        GameObject cContainer = new GameObject("CurrentDiceContainer");
        cContainer.transform.SetParent(canvasObj.transform, false);
        RectTransform cRt = cContainer.AddComponent<RectTransform>();
        cRt.anchoredPosition = new Vector2(0, -100);
        cRt.sizeDelta = new Vector2(800, 150);
        HorizontalLayoutGroup cLayout = cContainer.AddComponent<HorizontalLayoutGroup>();
        cLayout.childAlignment = TextAnchor.MiddleCenter;
        cLayout.spacing = 20;

        for (int i = 0; i < 6; i++)
        {
            GameObject face = new GameObject("CurrentFace_" + i);
            face.transform.SetParent(cContainer.transform, false);
            face.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            face.AddComponent<Image>();
            face.AddComponent<Button>();
        }

        // Next Stage Button
        GameObject nextBtnObj = new GameObject("NextStageButton");
        nextBtnObj.transform.SetParent(canvasObj.transform, false);
        RectTransform nxRt = nextBtnObj.AddComponent<RectTransform>();
        nxRt.anchoredPosition = new Vector2(0, -300);
        nxRt.sizeDelta = new Vector2(300, 80);
        Image nxImg = nextBtnObj.AddComponent<Image>();
        nxImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI 1/red_button.png");
        Button nextBtn = nextBtnObj.AddComponent<Button>();

        System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null) {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(nextBtnObj.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            Component tmp = txtObj.AddComponent(tmpType);
            tmpType.GetProperty("text").SetValue(tmp, "NEXT STAGE");
            tmpType.GetProperty("alignment").SetValue(tmp, 514);
            tmpType.GetProperty("fontSize").SetValue(tmp, 36f);
            tmpType.GetProperty("color").SetValue(tmp, Color.black);
        }

        // Add ForgeController
        GameObject controllerObj = new GameObject("ForgeController");
        var fc = controllerObj.AddComponent<ForgeController>();
        fc.rewardsContainer = rContainer.transform;
        fc.currentDiceContainer = cContainer.transform;
        fc.nextStageButton = nextBtn;

        // Load all possible faces
        string[] guids = AssetDatabase.FindAssets("t:DiceFaceData");
        List<DiceFaceData> faces = new List<DiceFaceData>();
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            faces.Add(AssetDatabase.LoadAssetAtPath<DiceFaceData>(p));
        }
        fc.allPossibleFaces = faces.ToArray();

        UnityEventTools.AddPersistentListener(nextBtn.onClick, fc.OnNextStageClicked);

        // Add GameManager placeholder for testing if missing
        if (GameObject.Find("GameManager") == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("PHASE 3 SETUP COMPLETE");
    }
}
