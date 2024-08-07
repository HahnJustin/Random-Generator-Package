using System.Collections;
using System.Collections.Generic;
using Dalichrome.RandomGenerator;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(GenerationManager))]
public class RandomGeneratorEditor : Editor
{
    private GenerationParamsObject genParamObj;
    private TextAsset textAsset;

    public override void OnInspectorGUI()
    {
        GenerationManager genMan = (GenerationManager)target;
        DrawDefaultInspector();
        // Draw fields for the serializable class
        /*
        GenerationParams genParams = genMan.GetParams();
        genParams.IsSeeded = EditorGUILayout.Toggle("Is Seeded", genParams.IsSeeded);

        if (genParams.IsSeeded)
        {
            genParams.Seed = (uint)Mathf.Max(0, EditorGUILayout.IntField("Seed", (int)genParams.Seed));
        }

        genParams.Width = Mathf.Max(1, EditorGUILayout.IntField("Width", genParams.Width));
        genParams.Height = Mathf.Max(1, EditorGUILayout.IntField("Height", genParams.Height));

        SerializedProperty genParamsProperty = serializedObject.FindProperty("generationParameters");
        SerializedProperty configsProperty = genParamsProperty.FindPropertyRelative("configs");
        EditorGUILayout.PropertyField(configsProperty, new GUIContent("Configs"), true);
        */

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Import Generation Params", EditorStyles.boldLabel);

        genParamObj = EditorGUILayout.ObjectField("", genParamObj, typeof(GenerationParamsObject), true) as GenerationParamsObject;

        if (GUILayout.Button("Import Gen Params Object") && genParamObj != null)
        {
            genMan.SetParams((GenerationParams)genParamObj.GenerationParams.Clone());
            genParamObj = null;
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        textAsset = EditorGUILayout.ObjectField("", textAsset, typeof(TextAsset), true) as TextAsset;

        if (GUILayout.Button("Import Gen Params Text") && textAsset != null)
        {
            string directoryPath = "Assets/GenParams";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"Created Directory {directoryPath}");
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{directoryPath}/{textAsset.name}.asset");

            string json = textAsset.text;
            GenerationParamsObject asset = CreateInstance<GenerationParamsObject>();
            JsonUtility.FromJsonOverwrite(json, asset);

            // Save the ScriptableObject as an asset
            AssetDatabase.CreateAsset(asset, assetPath);

            // Save all changes to the asset database
            AssetDatabase.SaveAssets();

            Debug.Log($"Created {textAsset.name}.asset, placed it in {directoryPath} and applied it to the {genMan.GetType()}");
            genMan.SetParams((GenerationParams)asset.GenerationParams.Clone());
            textAsset = null;

        }
    }
}
