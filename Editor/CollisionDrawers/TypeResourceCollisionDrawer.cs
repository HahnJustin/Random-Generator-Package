#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;

public abstract class TypeResourceCollisionDrawer<T, R> : PropertyDrawer
    where T : struct, Enum
    where R : AbstractUserData
{
    protected abstract string GetTypeName();
    protected abstract string GetResourceName();
    protected abstract string GetResourceFolderPath();

    // Enum Lookup
    private static readonly Lazy<Dictionary<int, string>> TypeByValue =
        new(() =>
        {
            var vals = (T[])Enum.GetValues(typeof(T));
            var map = new Dictionary<int, string>(vals.Length);
            foreach (var e in vals)
            {
                int key = ToInt(e);
                if (map.TryGetValue(key, out var existing))
                    map[key] = existing + ", " + e.ToString();
                else
                    map[key] = e.ToString();
            }
            return map;
        });

    private static readonly object _lock = new();

    // Resource Cache
    private static readonly Dictionary<(Type resType, string path), Dictionary<int, List<R>>> ResourceCache
        = new();

    // Subscribe once per closed generic
    static TypeResourceCollisionDrawer()
    {
        // invalidate on asset value edits
        AbstractUserData.AnyChanged += OnAnyUserDataChanged;

        // invalidate on undo/redo (values change without reimport)
        Undo.undoRedoPerformed += () => { lock (_lock) ResourceCache.Clear(); };

        // invalidate on project (re)imports/renames
        EditorApplication.projectChanged += () => { lock (_lock) ResourceCache.Clear(); };

        // invalidate on domain reload
        AssemblyReloadEvents.afterAssemblyReload += () => { lock (_lock) ResourceCache.Clear(); };
    }

    private static void OnAnyUserDataChanged(AbstractUserData obj)
    {
        // If the changed object is of our R type, zap entries for that type.
        if (obj is R)
        {
            lock (_lock)
            {
                // Remove only (typeof(R), *) entries
                var toRemove = new List<(Type, string)>();
                foreach (var key in ResourceCache.Keys)
                    if (key.resType == typeof(R)) toRemove.Add(key);

                foreach (var k in toRemove) ResourceCache.Remove(k);
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
            buckets = arr
                .GroupBy(r => r.GetId())
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
        float line = EditorGUIUtility.singleLineHeight;
        float helpH = 2f * line;

        if (TypeByValue.Value.TryGetValue(id, out var typeHit))
        {
            var r = new Rect(position.x, y, position.width, helpH);
            EditorGUI.HelpBox(r, $"ID {id} collides with {GetTypeName()}: {typeHit}", MessageType.Warning);
            y += helpH + EditorGUIUtility.standardVerticalSpacing;
        }

        if (TryGetOtherHits(id, GetResourceFolderPath(), property.serializedObject.targetObjects, out var resHit))
        {
            var r = new Rect(position.x, y, position.width, helpH);
            EditorGUI.HelpBox(r, $"ID {id} collides with {GetResourceName()}: {resHit}", MessageType.Warning);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUI.GetPropertyHeight(property, label, true);
        int id = property.intValue;

        int boxes = 0;
        if (TypeByValue.Value.ContainsKey(id)) boxes++;
        if (TryGetOtherHits(id, GetResourceFolderPath(), property.serializedObject.targetObjects, out _)) boxes++;

        if (boxes == 0) return h;

        float line = EditorGUIUtility.singleLineHeight;
        float helpH = 2f * line;
        return h + boxes * (helpH + EditorGUIUtility.standardVerticalSpacing);
    }

    private static bool TryGetOtherHits(
    int id, string path, UnityEngine.Object[] currentTargets, out string hitNames)
    {
        var buckets = GetResourceBuckets(path);
        hitNames = null;

        if (!buckets.TryGetValue(id, out var list) || list == null || list.Count == 0)
            return false;

        // Build a hash set of the current asset(s) being edited
        var selfSet = new HashSet<R>(currentTargets.OfType<R>());

        // Exclude the current targets
        var others = list.Where(r => !selfSet.Contains(r)).ToList();
        if (others.Count == 0)
            return false;

        hitNames = string.Join(", ", others.Select(o => o.ToString())); // or o.name
        return true;
    }


    private static int ToInt(T e) => UnsafeUtility.EnumToInt(e);
    // private static int ToInt(T e) => Convert.ToInt32(e); // fallback
}
#endif
