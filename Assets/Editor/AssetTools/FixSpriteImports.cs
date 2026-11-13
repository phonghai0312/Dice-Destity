using UnityEditor;
using UnityEngine;

public class FixSpriteImports
{
    [MenuItem("Tools/Fix Sprite Imports")]
    public static void Fix()
    {
        string[] paths = {
            "Assets/Art/UI/Branding/DICE & DESTINY.png",
            "Assets/Art/UI/Branding/Roll your fate — Forge your legend.png",
            "Assets/Art/UI/Branding/top_and_bottom_logo.png"
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
