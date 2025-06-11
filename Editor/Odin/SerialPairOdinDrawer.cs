#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using System;
using UnityEditor;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System.Reflection;

[DrawerPriority(0, 0, 0)]
public sealed class SerialPairOdinDrawer
         : OdinValueDrawer<SerialPair<int, int>>
{
    public override bool CanDrawTypeFilter(Type t)
        => t == typeof(SerialPair<int, int>);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var attr = Property.GetAttribute<TilePairDisplayAttribute>();
        if (attr == null)
        {
            CallNextDrawer(label);   // not ours
            return;
        }

        var pair = ValueEntry.SmartValue;

        EditorGUILayout.BeginHorizontal();

        var limitAttr = Property.GetAttribute<LimitTileLayerAttribute>();
        if (limitAttr == null)
        {
            var mi = Property.Info.GetMemberInfo();
            limitAttr = mi.GetCustomAttribute<LimitTileLayerAttribute>(true);
        }
        LayerType? layerFilter = limitAttr?.layer;

        // Key column
        if (attr.ShowKeyDropdown)
            pair.Key = TileDropdownOdinUtility.DrawSelector(GUIContent.none, pair.Key,
                                                 width: 120,
                                                 limit: layerFilter);
        else
            pair.Key = EditorGUILayout.IntField(pair.Key, GUILayout.Width(80));

        // Value column
        if (attr.ShowValueDropdown)
            pair.Value = TileDropdownOdinUtility.DrawSelector(GUIContent.none, pair.Value,
                                     width: 120,
                                     limit: layerFilter);
        else
            pair.Value = EditorGUILayout.IntField(pair.Value, GUILayout.Width(80));

        EditorGUILayout.EndHorizontal();
        ValueEntry.SmartValue = pair;
    }
}
#endif
