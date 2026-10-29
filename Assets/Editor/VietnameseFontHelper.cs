using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

public static class VietnameseFontHelper
{
    [MenuItem("Tools/Vietnamese Font/1. Create Arial SDF Asset")]
    public static void CreateArialSDF()
    {
        Font arial = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Arial.ttf");
        if (arial == null)
        {
            Debug.LogError("[VietnameseFontHelper] Assets/Fonts/Arial.ttf not found!");
            return;
        }

        Selection.activeObject = arial;
        EditorApplication.ExecuteMenuItem("Assets/Create/TextMeshPro/Font Asset/SDF");
        Debug.Log("[VietnameseFontHelper] Executed Create Arial SDF. Please check Assets/Fonts/Arial SDF.asset");
    }

    [MenuItem("Tools/Vietnamese Font/2. Link Arial SDF to Fallback")]
    public static void LinkArialSDF()
    {
        TMP_FontAsset arialSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Arial SDF.asset");
        if (arialSdf == null)
        {
            arialSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Arial SDF.asset");
        }

        if (arialSdf == null)
        {
            Debug.LogWarning("[VietnameseFontHelper] Could not find Arial SDF.asset. Please create it first via Step 1 or right-click Arial.ttf > Create > TextMeshPro > Font Asset!");
            return;
        }

        TMP_FontAsset libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (libSans != null)
        {
            if (libSans.fallbackFontAssetTable == null)
                libSans.fallbackFontAssetTable = new List<TMP_FontAsset>();

            libSans.fallbackFontAssetTable.RemoveAll(f => f == null || f.name.Contains("LiberationSans SDF - Fallback"));

            if (!libSans.fallbackFontAssetTable.Contains(arialSdf))
            {
                libSans.fallbackFontAssetTable.Insert(0, arialSdf);
            }
            EditorUtility.SetDirty(libSans);
            Debug.Log("[VietnameseFontHelper] Successfully linked Arial SDF to LiberationSans SDF fallback table!");
        }

        TMP_Settings tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
        if (tmpSettings != null)
        {
            SerializedObject so = new SerializedObject(tmpSettings);
            SerializedProperty fallbackProp = so.FindProperty("m_fallbackFontAssets");
            if (fallbackProp != null)
            {
                bool exists = false;
                for (int i = 0; i < fallbackProp.arraySize; i++)
                {
                    if (fallbackProp.GetArrayElementAtIndex(i).objectReferenceValue == arialSdf)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    fallbackProp.arraySize++;
                    fallbackProp.GetArrayElementAtIndex(fallbackProp.arraySize - 1).objectReferenceValue = arialSdf;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(tmpSettings);
                    Debug.Log("[VietnameseFontHelper] Successfully added Arial SDF to TMP Settings fallback list!");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VietnameseFontHelper] Vietnamese Font setup complete!");
    }
}
