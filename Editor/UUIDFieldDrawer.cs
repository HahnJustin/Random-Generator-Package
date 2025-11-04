// UUIDFieldDrawer.cs (Editor asm; can reference UserData)
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;     // attribute
using Dalichrome.RandomGenerator.UserData;    // UUIDScriptableObject & UUIDFolderAttribute

[CustomPropertyDrawer(typeof(UUIDFieldAttribute), true)]
public class UUIDFieldDrawer : PropertyDrawer
{
    private static double _lastScan;
    private static readonly Dictionary<Type, List<(string name, string uuid)>> _cache = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.HelpBox(position, "[UUIDField] must be on a string.", MessageType.Error);
            return;
        }

        var attr = (UUIDFieldAttribute)attribute;
        var type = ResolveType(attr.AssetTypeName);
        if (type == null)
        {
            EditorGUI.HelpBox(position, $"UUIDField: type not found: {attr.AssetTypeName}", MessageType.Error);
            return;
        }

        var list = GetList(type, attr);
        var options = new string[list.Count + 1];
        options[0] = "(None)";

        int idx = 0;
        for (int i = 0; i < list.Count; i++)
        {
            options[i + 1] = list[i].name;           // file/asset name
            if (property.stringValue == list[i].uuid) idx = i + 1;
        }

        EditorGUI.BeginProperty(position, label, property);
        int newIdx = EditorGUI.Popup(position, label.text, idx, options);
        if (newIdx != idx)
            property.stringValue = (newIdx == 0) ? string.Empty : list[newIdx - 1].uuid;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;

    private static List<(string name, string uuid)> GetList(Type assetType, UUIDFieldAttribute attr)
    {
        if (EditorApplication.timeSinceStartup - _lastScan < 2.0 &&
            _cache.TryGetValue(assetType, out var cached))
        {
            return cached;
        }

        _lastScan = EditorApplication.timeSinceStartup;

        // Folders: attribute override -> [UUIDFolder] -> default Type.Name
        var folders = (attr.FoldersOverride != null && attr.FoldersOverride.Length > 0)
            ? attr.FoldersOverride
            : ResolveFoldersFromType(assetType);

        var roots = new List<string>();
        foreach (var f in folders)
        {
            var root = $"Assets/Resources/{f}";
            if (System.IO.Directory.Exists(root)) roots.Add(root);
        }

        var filter = $"t:{assetType.Name}";
        var guids = (roots.Count > 0) ? AssetDatabase.FindAssets(filter, roots.ToArray())
                                       : AssetDatabase.FindAssets(filter);

        var results = new List<(string name, string uuid)>();
        bool isUuidSO = typeof(UUIDScriptableObject).IsAssignableFrom(assetType);

        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var obj = AssetDatabase.LoadAssetAtPath(path, assetType) as UnityEngine.Object;
            if (obj == null) continue;

            string name = obj.name;
            string uuid = null;

            if (isUuidSO)
            {
                uuid = (obj as UUIDScriptableObject)?.Uuid;
            }
            else
            {
                // Fallback: reflect a public string property named "Uuid"
                var p = assetType.GetProperty("Uuid", BindingFlags.Instance | BindingFlags.Public);
                uuid = p?.GetValue(obj) as string;
            }

            if (string.IsNullOrEmpty(uuid)) continue;
            results.Add((name, uuid));
        }

        results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        _cache[assetType] = results;
        return results;
    }

    private static Type ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        var t = Type.GetType(typeName); // assembly-qualified works directly
        if (t != null) return t;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(typeName);
            if (t != null) return t;
        }
        return null;
    }

    private static string[] ResolveFoldersFromType(Type t)
    {
        var a = (UUIDFolderAttribute)Attribute.GetCustomAttribute(t, typeof(UUIDFolderAttribute));
        return (a?.Folders?.Length ?? 0) > 0 ? a.Folders : new[] { t.Name };
    }
}
#endif
