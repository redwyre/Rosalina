﻿#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class RosalinaClearBindingsMenuItem
{
    private const string MenuItemPath = "Assets/Rosalina/Clear Bindings";

    [MenuItem(MenuItemPath, true)]
    public static bool ClearBindingsValidation()
    {
        return RosalinaSettings.instance.IsEnabled && Selection.activeObject != null && Selection.activeObject.GetType() == typeof(VisualTreeAsset);
    }

    [MenuItem(MenuItemPath, priority = 21)]
    public static void ClearBindings()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var document = new UIDocumentAsset(assetPath);

        try
        {
            string generatedBindingsScriptPath = RosalinaGenerator.BuildAutoGeneratedFilePath(document);

            if (File.Exists(generatedBindingsScriptPath))
            {
                File.Delete(generatedBindingsScriptPath);
                AssetDatabase.Refresh();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e, Selection.activeObject);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif