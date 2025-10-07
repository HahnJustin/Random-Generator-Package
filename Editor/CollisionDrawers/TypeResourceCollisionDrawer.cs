#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager; // PackageInfo

/// Collides int IDs against other ScriptableObjects of type R found in Resources/<GetResourceFolderPath()>
public abstract class ResourceCollisionDrawer<R> : PropertyDrawer where R : AbstractUserData
{
    protected abstract string GetResourceName();        // e.g., "TileLayer" or "TileObject"
    protected abstract string GetResourceFolderPath();  // e.g., "TileLayers" (Resources subpath)

    private static readonly object _lock = new();

    // (type,path) Å® id Å® list<R>
    private static readonly Dictionary<(Type resType, string path), Dictionary<int, List<R>>> ResourceCache = new();

    static ResourceCollisionDrawer()
    {
        AbstractUserData.AnyChanged += OnAnyUserDataChanged;
        Undo.undoRedoPerformed += () => { lock (_lock) ResourceCache.Clear(); };
        EditorApplication.projectChanged += () => { lock (_lock) ResourceCache.Clear(); };
        AssemblyReloadEvents.afterAssemblyReload += () => { lock (_lock) ResourceCache.Clear(); };
    }

    private static void OnAnyUserDataChanged(AbstractUserData obj)
    {
        if (obj is R)
        {
            lock (_lock)
            {
                var remove = new List<(Type, string)>();
                foreach (var k in ResourceCache.Keys)
                    if (k.resType == typeof(R)) remove.Add(k);
                foreach (var k in remove) ResourceCache.Remove(k);
            }
        }
    }

    private static Dictionary<int, List<R>> GetResourceBuckets(string path)
    {
        var key = (typeof(R), path ?? string.Empty);
        lock (_lock)
        {
            if (ResourceCache.TryGetValue(key, out var buckets))
                return buckets;

            var arr = Resources.LoadAll<R>(path);
            buckets = arr.GroupBy(r => r.GetId())
                         .ToDictionary(g => g.Key, g => g.ToList());
            ResourceCache[key] = buckets;
            return buckets;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        int id = property.intValue;
        float y = position.y + EditorGUI.GetPropertyHeight(property, label, true)
                  + EditorGUIUtility.standardVerticalSpacing;
        float boxH = 2f * EditorGUIUtility.singleLineHeight;

        if (TryGetOtherHits(id, GetResourceFolderPath(), property.serializedObject.targetObjects, out var hitList))
        {
            // Include the Resources subpath in the message so you know which set was scanned.
            string msg = $"ID {id} collides with {GetResourceName()} in '{GetResourceFolderPath()}': {string.Join("; ", hitList)}";
            var r = new Rect(position.x, y, position.width, boxH);
            EditorGUI.HelpBox(r, msg, MessageType.Warning);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUI.GetPropertyHeight(property, label, true);
        return TryGetOtherHits(property.intValue, GetResourceFolderPath(), property.serializedObject.targetObjects, out _)
             ? h + (2f * EditorGUIUtility.singleLineHeight) + EditorGUIUtility.standardVerticalSpacing
             : h;
    }

    // Builds "Group/Name" per conflicting asset (Group is "Assets" or package display name)
    private static bool TryGetOtherHits(int id, string path, UnityEngine.Object[] currentTargets, out List<string> hits)
    {
        hits = null;
        var buckets = GetResourceBuckets(path);

        if (!buckets.TryGetValue(id, out var list) || list == null || list.Count == 0)
            return false;

        var self = new HashSet<R>(currentTargets.OfType<R>());
        var others = list.Where(r => !self.Contains(r)).ToList();
        if (others.Count == 0) return false;

        hits = others.Select(o => $"{GetSourceTabName(o)}/{o.name}").ToList();
        return true;
    }

    private static string GetSourceTabName(ScriptableObject so)
    {
        var path = AssetDatabase.GetAssetPath(so);
        if (string.IsNullOrEmpty(path)) return "Assets";
        if (path.StartsWith("Assets/"))
            return "Assets";

        if (path.StartsWith("Packages/"))
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
            if (info != null && !string.IsNullOrWhiteSpace(info.displayName)) return info.displayName;
            if (info != null) return info.name;

            var m = Regex.Match(path, @"^Packages/([^/]+)/");
            return m.Success ? m.Groups[1].Value : "Package";
        }
        return "Assets";
    }
}
#endif
