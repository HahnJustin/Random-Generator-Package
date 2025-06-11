#if ODIN_INSPECTOR
using System;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Dalichrome.RandomGenerator.Configs;

internal static class ConditionEval
{
    public static bool IsVisible(InspectorProperty prop)
    {
        var cond = prop.GetAttribute<ConditionAttribute>();
        if (cond == null) return true;

        object host = prop.ParentValues.Count > 0 ? prop.ParentValues[0] : null;
        if (host == null) return true;

        var t = host.GetType();
        FieldInfo fi = t.GetField(cond.DependentPropertyName,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        PropertyInfo pi = t.GetProperty(cond.DependentPropertyName,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object val = fi != null ? fi.GetValue(host) : pi?.GetValue(host);
        return Equals(val, cond.CompareAgainst);
    }
}
#endif
