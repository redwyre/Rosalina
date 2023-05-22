﻿#if UNITY_EDITOR
using System.IO;
using UnityEngine;

internal static class RosalinaGenerator
{
    /// <summary>
    /// Creates the auto generated code file path based on the given source path and output file name.
    /// If BindingOutputPath in the settings is empty, will place beside the source asset.
    /// </summary>
    /// <param name="sourceAssetPath">Source asset path.</param>
    /// <param name="outputFileName">Output file name.</param>
    /// <returns>Auto generated file path.</returns>
    public static string BuildAutoGeneratedFilePath(string sourceAssetPath, string outputFileName)
    {
        string outputPath = RosalinaSettings.instance.BindingOutputPath;

        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.GetDirectoryName(sourceAssetPath);
        }
        else
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
        }

        return Path.Combine(outputPath, outputFileName);
    }

    /// <summary>
    /// Generates a C# script containing the bindings of the given UI document.
    /// </summary>
    /// <param name="document">UI Document.</param>
    /// <param name="outputFile">Output file.</param>
    /// <returns>Rosalina generation result.</returns>
    public static void GenerateBindings(UIDocumentAsset document, string outputFile)
    {
        IRosalinaGeneartor generator = document.UxmlDocument.IsEditorExtension ? 
            new RosalinaEditorWindowBindingsGeneartor() : 
            new RosalinaBindingsGenerator();

        Debug.Log($"[Rosalina]: Generating UI bindings for {document.FullPath}");

        RosalinaGenerationResult result = generator.Generate(document);
        result.Save(outputFile);

        Debug.Log($"[Rosalina]: Done generating: {document.Name} (output: {outputFile})");
    }

    /// <summary>
    /// Geneartes a C# script for the UI logic.
    /// </summary>
    /// <param name="document">UI Document asset information.</param>
    /// <param name="outputFile">Output file.</param>
    /// <returns>Rosalina generation result.</returns>
    public static void GenerateScript(UIDocumentAsset document, string outputFile)
    {
        IRosalinaGeneartor generator = document.UxmlDocument.IsEditorExtension ?
            new RosalinaEditorWindowScriptGenerator() :
            new RosalinaScriptGenerator();

        Debug.Log($"[Rosalina]: Generating UI script for {outputFile}");

        RosalinaGenerationResult result = generator.Generate(document);
        result.Save(outputFile);

        Debug.Log($"[Rosalina]: Done generating: {document.Name} (output: {outputFile})");
    }
}
#endif