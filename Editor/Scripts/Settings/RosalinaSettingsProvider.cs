using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class RosalinaSettingsProvider : SettingsProvider
{
    public RosalinaSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
        : base(path, scopes, keywords)
    {
    }

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        var settings = RosalinaSettings.instance;
        var serialized = new SerializedObject(settings);

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.eastylabs.rosalina/Editor/UI/rosalina-settings-window.uxml");

        visualTree.CloneTree(rootElement);

        var body = rootElement.Q("Body");

        var property = serialized.GetIterator();
        while (property.NextVisible(true))
        {
            if (SkipField(property.propertyPath)) { continue; }

            var propertyField = new PropertyField(property, ObjectNames.NicifyVariableName(property.name));
            body.Add(propertyField);
        }

        rootElement.Bind(serialized);

        base.OnActivate(searchContext, rootElement);
    }

    [SettingsProvider]
    public static SettingsProvider CreateRosalinaSettingsProvider()
    {
        var provider = new RosalinaSettingsProvider("Project/Rosalina Settings", SettingsScope.Project);
        return provider;
    }

    static bool SkipField(string fieldName)
    {
        return fieldName == "m_Script";
    }
}

