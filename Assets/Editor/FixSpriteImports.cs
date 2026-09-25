using UnityEditor;
using UnityEngine;

public class FixSpriteImports
{
    [MenuItem("Tools/Fix Sprite Imports")]
    public static void Fix()
    {
        string[] paths = {
            "Assets/Sprites/UI 1/DICE & DESTINY.png",
            "Assets/Sprites/UI 1/Roll your fate — Forge your legend.png",
            "Assets/Sprites/UI 1/top_and_bottom_logo.png"
        };

        foreach (string path in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                Debug.Log("Fixed import settings for: " + path);
            }
            else
            {
                Debug.LogWarning("Could not find: " + path);
            }
        }
    }
}
