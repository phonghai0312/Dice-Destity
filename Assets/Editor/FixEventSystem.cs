using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixEventSystem
{
    [MenuItem("Tools/Fix UI Input")]
    public static void FixInput()
    {
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/CombatScene.unity", "Assets/Scenes/TheForge.unity" };

        foreach (string path in scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(path);
            GameObject esObj = GameObject.Find("EventSystem");
            if (esObj != null)
            {
                // Remove all input modules
                var oldModules = esObj.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
                foreach (var m in oldModules)
                {
                    Object.DestroyImmediate(m);
                }

                System.Type newModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (newModuleType != null)
                {
                    Component module = esObj.AddComponent(newModuleType);
                    System.Reflection.MethodInfo assignMethod = newModuleType.GetMethod("AssignDefaultActions");
                    if (assignMethod != null)
                    {
                        assignMethod.Invoke(module, null);
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("UI Input Fixed for all scenes!");
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }
}
