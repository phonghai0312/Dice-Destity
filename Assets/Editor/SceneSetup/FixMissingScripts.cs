using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class FixMissingScripts
{
    [MenuItem("Tools/Fix GameManager & Missing Scripts")]
    public static void Fix()
    {
        var s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(null);
        
        // Remove all objects named GameManager to avoid duplicates/broken ones
        var gms = GameObject.FindObjectsOfType<GameManager>(true);
        foreach (var gm in gms) Object.DestroyImmediate(gm.gameObject);
        
        GameObject oldGm = GameObject.Find("GameManager");
        if (oldGm != null) Object.DestroyImmediate(oldGm);

        // Create fresh
        GameObject newGm = new GameObject("GameManager");
        newGm.AddComponent<GameManager>();

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        Debug.Log("Fixed MainMenu GameManager!");
    }
}
