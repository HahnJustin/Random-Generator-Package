using Dalichrome.RandomGenerator.UserData;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Structure))]
public class StructureInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if (GUILayout.Button("Open in Structure Painter"))
            StructureEditorWindow.Open((Structure)target);
    }
}