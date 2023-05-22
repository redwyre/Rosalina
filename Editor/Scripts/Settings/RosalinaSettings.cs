﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[FilePath("ProjectSettings/RosalinaSettings.asset", FilePathAttribute.Location.ProjectFolder)]
public class RosalinaSettings : ScriptableSingleton<RosalinaSettings>
{
    [SerializeField]
    private string m_BindingOutputPath = "Assets/Rosalina/AutoGenerated/";

    public string BindingOutputPath => m_BindingOutputPath;

    private void OnDisable()
    {
        Save(true);
    }

    internal SerializedObject GetSerializedObject()
    {
        return new SerializedObject(this);
    }
}