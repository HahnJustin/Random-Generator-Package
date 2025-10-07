#if ODIN_INSPECTOR && UNITY_EDITOR
using System;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public abstract class GenericIdOdinDrawerBase<TEnum, TRes, TMarker> : OdinValueDrawer<int>
    where TEnum : struct, Enum
    where TRes : ScriptableObject // or AbstractUserData if you prefer
    where TMarker : PropertyAttribute
{
    protected abstract string ResourcesPath { get; }
    protected virtual string EnumPrefix => "enum/";
    protected virtual string AssetPrefix => "asset/";

    public override bool CanDrawTypeFilter(Type t) => t == typeof(int);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Only handle fields with our marker attribute
        if (Property.GetAttribute<TMarker>() == null)
        {
            CallNextDrawer(label);
            return;
        }

        var entry = GenericIdDropdownCache.GetOrBuild(
            typeof(TEnum), typeof(TRes), ResourcesPath, EnumPrefix, AssetPrefix);

        var items = entry.Items;
        if (items.Count == 0)
        {
            CallNextDrawer(label);
            return;
        }

        int current = ValueEntry.SmartValue;
        int idx = Math.Max(0, items.FindIndex(i => i.id == current));

        // Layout (no BeginProperty/EndProperty in Odin drawers)
        var r = EditorGUILayout.GetControlRect();
        var lab = label ?? Property.Label ?? GUIContent.none;

        // Draw label and popup
        float lw = EditorGUIUtility.labelWidth;
        var labelR = new Rect(r.x, r.y, lw, r.height);
        var fieldR = new Rect(r.x + lw, r.y, r.width - lw, r.height);

        EditorGUI.LabelField(labelR, lab);
        int newIdx = EditorGUI.Popup(fieldR, idx, items.Select(i => i.label).ToArray());
        ValueEntry.SmartValue = items[newIdx].id;
    }
}
#endif
