#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;

[DrawerPriority(0, 0, 0)]
public sealed class TileIdOdinDrawer : OdinValueDrawer<int>
{
    public override bool CanDrawTypeFilter(Type t) => t == typeof(int);

    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (Property.GetAttribute<TileDisplayAttribute>() == null)
        {
            CallNextDrawer(label);                    // plain int
            return;
        }

        // optional layer filter
        var lim = Property.GetAttribute<LimitTileLayerAttribute>() ??
                  Property.Info.GetMemberInfo()
                          .GetCustomAttribute<LimitTileLayerAttribute>(true);

        /*──── calculate rects ─────────────────────────────────────────*/
        Rect total = EditorGUILayout.GetControlRect();             // full row
        Rect labelR = new(total.x, total.y,
                           EditorGUIUtility.labelWidth,
                           total.height);

        // leave 4-pixel margin on the right so it never pokes out
        Rect fieldR = new(labelR.xMax,
                           total.y,
                           total.width - labelR.width - 4f,
                           total.height);

        /*──── draw ───────────────────────────────────────────────────*/
        EditorGUI.LabelField(labelR, label);
        ValueEntry.SmartValue =
            TileDropdownOdinUtility.DrawSelector(fieldR,
                                                 ValueEntry.SmartValue,
                                                 lim?.layer);
    }
}
#endif
