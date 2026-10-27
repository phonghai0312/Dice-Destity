using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// RebuildStageMapScene — Tạo / làm mới Scene StageMap.unity và tự động đăng ký vào Build Settings.
/// Menu: Tools → Rebuild Stage Map Scene
/// </summary>
public class RebuildStageMapScene
{
    [MenuItem("Tools/Rebuild Stage Map Scene")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode first!");
            return;
        }

        string scenePath = "Assets/Scenes/StageMap.unity";
        Scene scene = System.IO.File.Exists(scenePath)
            ? EditorSceneManager.OpenScene(scenePath)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Xóa các root object cũ để tạo sạch
        foreach (GameObject rootGo in scene.GetRootGameObjects())
        {
            Object.DestroyImmediate(rootGo);
        }

        // 1. Main Camera
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f, 1f);
        cam.orthographic = true;
        cam.depth = -1;
        camGo.AddComponent<AudioListener>();

        // 2. EventSystem
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        System.Type inputSysType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSysType != null)
        {
            Component mod = esGo.AddComponent(inputSysType);
            inputSysType.GetMethod("AssignDefaultActions")?.Invoke(mod, null);
        }
        else
        {
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 3. Canvas + PreviewBackground
        GameObject cvGo = new GameObject("Canvas");
        Canvas cv = cvGo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = cvGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        cvGo.AddComponent<GraphicRaycaster>();

        Sprite mapSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/World_Map_Overworld.png");
        GameObject bgGo = new GameObject("PreviewBackground");
        bgGo.transform.SetParent(cvGo.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.sprite = mapSpr;
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;

        // 4. GameManager
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.stageEnemies = new EnemyData[]
        {
            AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Stage1_Goblin.asset"),
            AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Stage2_Orc.asset"),
            AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Stage3_Minotaur.asset")
        };
        gm.currentStageIndex = 2;
        gm.heroMapStageIndex = 1;
        gm.highestUnlockedStageIndex = 2;
        gm.lastClearedStageIndex = 1;
        gm.pendingForgeUpgrades = 1;

        // 5. StageMapController
        GameObject ctrlGo = new GameObject("StageMapController");
        StageMapController ctrl = ctrlGo.AddComponent<StageMapController>();
        ctrl.mapBackgroundSprite = mapSpr;
        ctrl.heroTokenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Knight_Hero_Facing_Right.png");
        ctrl.goblinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Goblin_Assassin.png");
        ctrl.orcSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Orc_Berserker.png");
        ctrl.minotaurSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Minotaur_Boss.png");
        ctrl.forgeIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Fire.png");
        ctrl.attackIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Attack.png");
        ctrl.shieldIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Dice_Face_Shield.png");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        // Đảm bảo StageMap.unity có trong Build Settings
        EnsureSceneInBuildSettings(scenePath);
        Debug.Log("=== StageMap Scene Rebuilt & Added to Build Settings! ===");
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool exists = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath)
            {
                s.enabled = true;
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
