using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

/// <summary>
/// Fixes the CombatScene:
/// 1. Fixes white boxes on PlayerArea / EnemyArea  
/// 2. Fixes EventSystem to use new Input System
/// 3. Ensures all dice have GraphicRaycaster target available
/// </summary>
public class FixCombatSceneFull
{
    [MenuItem("Tools/Fix Combat Scene Full")]
    public static void DoFix()
    {
        if (EditorApplication.isPlaying) {
            Debug.LogError("Tắt Play mode trước!");
            return;
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");

        // ─── 1. Fix EventSystem ───────────────────────────────────────────
        GameObject esObj = GameObject.Find("EventSystem");
        if (esObj != null)
        {
            var oldModules = esObj.GetComponents<UnityEngine.EventSystems.BaseInputModule>();
            foreach (var m in oldModules) Object.DestroyImmediate(m);

            System.Type newModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModule != null)
            {
                var module = esObj.AddComponent(newModule);
                var assign = newModule.GetMethod("AssignDefaultActions");
                if (assign != null) assign.Invoke(module, null);
                Debug.Log("EventSystem: InputSystemUIInputModule assigned with default actions.");
            }
        }

        // ─── 2. Ensure Canvas has GraphicRaycaster ────────────────────────
        GameObject canvasObj = GameObject.Find("MainCanvas");
        if (canvasObj != null)
        {
            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
                canvasObj.AddComponent<GraphicRaycaster>();
        }

        // ─── 3. Fix PlayerArea white box ──────────────────────────────────
        FixCharacterArea("PlayerArea", "Assets/Sprites/Characters 1/player.png");
        FixCharacterArea("EnemyArea",  "Assets/Sprites/Characters 1/goblin.jpg");

        // ─── 4. Fix any remaining white Image boxes in the scene ─────────
        // Find all Image components that are pure white and have no sprite - disable them
        Image[] allImages = Object.FindObjectsOfType<Image>();
        foreach (var img in allImages)
        {
            if (img.sprite == null && img.color == Color.white && img.gameObject.name != "Background")
            {
                // Check if it has children (it's a container, not a visible UI element)
                if (img.transform.childCount > 0)
                {
                    img.enabled = false;
                    Debug.Log("Disabled white placeholder: " + img.gameObject.name);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Combat Scene Fixed!");

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    static void FixCharacterArea(string areaName, string fallbackSpritePath)
    {
        GameObject area = GameObject.Find(areaName);
        if (area == null) return;

        // Disable the background Image on the area itself (the white box)
        Image areaImg = area.GetComponent<Image>();
        if (areaImg != null) areaImg.enabled = false;

        // Find child named "Sprite" or "CharacterSprite" and fix it
        Transform spriteChild = area.transform.Find("Sprite");
        if (spriteChild == null) spriteChild = area.transform.Find("CharacterSprite");
        if (spriteChild == null) spriteChild = area.transform.Find(areaName.Replace("Area", "Sprite"));

        if (spriteChild != null)
        {
            Image img = spriteChild.GetComponent<Image>();
            if (img != null)
            {
                // Make it fill the parent
                RectTransform rt = spriteChild.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                img.preserveAspect = true;

                // Load fallback if sprite missing
                if (img.sprite == null)
                {
                    // Try finding any character sprite
                    string[] allSprites = new string[] {
                        "Assets/Sprites/Characters 1/goblin.jpg",
                        "Assets/Sprites/Characters 1/orc.jpg",
                        "Assets/Sprites/Characters 1/mino.jpg",
                        "Assets/Sprites/Characters 1/player.png",
                        fallbackSpritePath
                    };
                    foreach (var path in allSprites)
                    {
                        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (s != null) { img.sprite = s; break; }
                    }
                }
            }
        }
    }
}
