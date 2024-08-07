using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(GenerationParams))]
public class GenerationParamsEditor : Editor
{
    public override void OnInspectorGUI()
    {

        DrawDefaultInspector();
        /*
        GenerationParams genParams = (GenerationParams)target;
        genParams.IsSeeded = EditorGUILayout.Toggle("Is Seeded", genParams.IsSeeded);

        if (genParams.IsSeeded)
        {
            genParams.Seed = (uint)Mathf.Max(0, EditorGUILayout.IntField("Seed", (int)genParams.Seed));
        }

        genParams.Width = Mathf.Max(1, EditorGUILayout.IntField("Width", genParams.Width));
        genParams.Height = Mathf.Max(1, EditorGUILayout.IntField("Height", genParams.Height));

        SerializedProperty configsProperty = serializedObject.FindProperty("configs");
        EditorGUILayout.PropertyField(configsProperty, new GUIContent("Configs"), true);
        */
    }
}
