using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class Phase1Setup
{
    [MenuItem("Tools/Setup Phase 1")]
    public static void DoSetup()
    {
        AssetDatabase.Refresh();

        string samplePath = "Assets/Scenes/SampleScene.unity";
        if (System.IO.File.Exists(samplePath)) {
            AssetDatabase.RenameAsset(samplePath, "CombatScene");
        }

        string menuPath = "Assets/Scenes/MainMenu.unity";
        if (!System.IO.File.Exists(menuPath)) {
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), menuPath);
        }

        string forgePath = "Assets/Scenes/TheForge.unity";
        if (!System.IO.File.Exists(forgePath)) {
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), forgePath);
        }

        List<EditorBuildSettingsScene> newScenes = new List<EditorBuildSettingsScene>();
        newScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true));
        newScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/CombatScene.unity", true));
        newScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/TheForge.unity", true));

        EditorBuildSettings.scenes = newScenes.ToArray();
        EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");
        Debug.Log("PHASE 1 SETUP COMPLETE");
    }
}
