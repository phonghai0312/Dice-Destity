using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class VietnameseFontHelper
{
    private const string VIETNAMESE_CHARS =
        "aàáảãạăằắẳẵặâầấẩẫậ" +
        "eèéẻẽẹêềếểễệ" +
        "iìíỉĩị" +
        "oòóỏõọôồốổỗộơờớởỡợ" +
        "uùúủũụưừứửữự" +
        "yỳýỷỹỵ" +
        "dđ" +
        "AÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬ" +
        "EÈÉẺẼẸÊỀẾỂỄỆ" +
        "IÌÍỈĨỊ" +
        "OÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢ" +
        "UÙÚỦŨỤƯỪỨỬỮỰ" +
        "YỲÝỶỸỴ" +
        "DĐ" +
        "0123456789" +
        " –—…“”‘’•/\\()[]{}<>:;,.*+-%=!#?@$^_`|~";

    static VietnameseFontHelper()
    {
        EditorApplication.delayCall += EnsureVietnameseFontSetup;
    }

    [MenuItem("Tools/Vietnamese Font/Setup Full Vietnamese Font Fallback")]
    public static void EnsureVietnameseFontSetup()
    {
        // 1. Delete redundant 1x1 dummy Arial SDF asset if present
        string dummyPath = "Assets/Fonts/Arial SDF.asset";
        if (System.IO.File.Exists(dummyPath))
        {
            AssetDatabase.DeleteAsset(dummyPath);
            Debug.Log("[VietnameseFontHelper] Removed dummy Assets/Fonts/Arial SDF.asset");
        }

        // 2. Load the genuine Arial SDF asset in TMP Resources
        string realPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Arial SDF.asset";
        TMP_FontAsset arialSdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(realPath);
        if (arialSdf == null)
        {
            Debug.LogWarning($"[VietnameseFontHelper] Could not find {realPath}!");
            return;
        }

        // Enable multi-atlas support for dynamic expansion
        arialSdf.isMultiAtlasTexturesEnabled = true;

        // 3. Pre-rasterize all Vietnamese characters into Arial SDF atlas
        string missing;
        bool added = arialSdf.TryAddCharacters(VIETNAMESE_CHARS, out missing);
        if (added)
        {
            Debug.Log("[VietnameseFontHelper] Successfully pre-added all Vietnamese glyphs to Arial SDF!");
        }
        else
        {
            Debug.LogWarning($"[VietnameseFontHelper] Some characters could not be added: {missing}");
        }

        // 4. Configure LiberationSans SDF fallback table
        string libPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        TMP_FontAsset libSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(libPath);
        if (libSans != null)
        {
            if (libSans.fallbackFontAssetTable == null)
                libSans.fallbackFontAssetTable = new List<TMP_FontAsset>();

            libSans.fallbackFontAssetTable.RemoveAll(f => f == null || f.name.Contains("LiberationSans SDF - Fallback") || f.name == "Arial SDF" && AssetDatabase.GetAssetPath(f) != realPath);

            if (!libSans.fallbackFontAssetTable.Contains(arialSdf))
            {
                libSans.fallbackFontAssetTable.Insert(0, arialSdf);
            }
            EditorUtility.SetDirty(libSans);
            Debug.Log("[VietnameseFontHelper] Linked real Arial SDF to LiberationSans SDF fallback table!");
        }

        // 5. Configure TMP Settings fallbacks
        TMP_Settings tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
        if (tmpSettings != null)
        {
            SerializedObject so = new SerializedObject(tmpSettings);
            SerializedProperty fallbackProp = so.FindProperty("m_fallbackFontAssets");
            if (fallbackProp != null)
            {
                // Remove nulls and invalid entries
                for (int i = fallbackProp.arraySize - 1; i >= 0; i--)
                {
                    var elem = fallbackProp.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (elem == null || (elem.name == "Arial SDF" && AssetDatabase.GetAssetPath(elem) != realPath))
                    {
                        fallbackProp.DeleteArrayElementAtIndex(i);
                    }
                }

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
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmpSettings);
            }
        }

        EditorUtility.SetDirty(arialSdf);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VietnameseFontHelper] Vietnamese Font setup completed successfully!");
    }
}
