#if ODIN_INSPECTOR
using System;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

[DrawerPriority(0, 0, 0)]
public sealed class ConditionOdinDrawer : OdinAttributeDrawer<ConditionAttribute>
{
    public override bool CanDrawTypeFilter(Type _) => true;

    protected override void DrawPropertyLayout(GUIContent label)
    {
        if (ShouldShow(Property, Attribute))
        {
            // Draw the next drawer in the chain (default field, custom drawers, etc.)
            CallNextDrawer(label);
        }
        // else: do nothing  ¨ height becomes 0 and the field is hidden
    }

    /* ------------------------------------------------------------- */
    private static bool ShouldShow(InspectorProperty prop, ConditionAttribute cond)
    {
        // Traverse upwards to the object that owns this property
        object host = prop.ParentValues.Count > 0 ? prop.ParentValues[0] : null;
        if (host == null) return true;

        var t = host.GetType();

        // Try a field first
        var fi = t.GetField(cond.DependentPropertyName,
                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object val = fi != null
            ? fi.GetValue(host)
            : t.GetProperty(cond.DependentPropertyName,
                 BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 ?.GetValue(host);

        return Equals(val, cond.CompareAgainst);
    }
}
#endif
