#if ODIN_INSPECTOR && UNITY_EDITOR
using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public abstract class GenericIdOdinDrawerBase<TRes, TMarker> : OdinValueDrawer<int>
    where TRes : ScriptableObject
    where TMarker : PropertyAttribute
{
    protected abstract string ResourcesPath { get; }

    public override bool CanDrawTypeFilter(Type t) => t == typeof(int);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (Property.GetAttribute<TMarker>() == null)
        {
            CallNextDrawer(label);
            return;
        }

        var entry = GenericIdDropdownCache.GetOrBuild(typeof(TRes), ResourcesPath);
        var items = entry.Items;
        if (items.Count == 0) { CallNextDrawer(label); return; }

        // (optional) apply a filter using entry.CategoryOf[...] if you want

        int current = ValueEntry.SmartValue;
        int idx = Math.Max(0, items.FindIndex(i => i.id == current));

        // AFTER (safe)
        Rect r = EditorGUILayout.GetControlRect();

        var labelRect = new Rect(r.x, r.y, EditorGUIUtility.labelWidth, r.height);
        var fieldRect = new Rect(labelRect.xMax, r.y, r.width - labelRect.width, r.height);

        // Draw label
        EditorGUI.LabelField(labelRect, label ?? GUIContent.none);

        // Draw popup
        var names = items.Select(i => i.label).ToArray();
        int newIdx = EditorGUI.Popup(fieldRect, idx, names);

        // Apply
        ValueEntry.SmartValue = items[newIdx].id;
    }
}
#endif
