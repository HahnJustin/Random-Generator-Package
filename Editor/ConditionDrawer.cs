#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using System;
using System.Collections;
using System.Text.RegularExpressions;

[CustomPropertyDrawer(typeof(ConditionAttribute))]
public class ConditionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        if (ConditionMet(prop, (ConditionAttribute)attribute))
            EditorGUI.PropertyField(pos, prop, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
    {
        return ConditionMet(prop, (ConditionAttribute)attribute)
            ? EditorGUI.GetPropertyHeight(prop, label, true)
            : -EditorGUIUtility.standardVerticalSpacing;   // ← removes residual gap
    }

    /* ---------- helpers (same as previous robust version) ---------- */
    private static bool ConditionMet(SerializedProperty prop, ConditionAttribute cond)
    {
        object host = GetHostObject(prop);
        if (host == null) return true;

        object current = ReflectionHelper.GetFieldValue(host, cond.DependentPropertyName);
        if (current is UnityEngine.Object uo && uo == null) current = null;

        return EqualsRobust(current, cond.CompareAgainst);
    }

    private static object GetHostObject(SerializedProperty prop)
    {
        object obj = prop.serializedObject.targetObject;
        string path = prop.propertyPath;
        int lastDot = path.LastIndexOf('.');
        if (lastDot < 0) return obj;

        string[] elements = path[..lastDot].Split('.');
        Regex arrayElem = new(@"^data\[(\d+)\]$");

        foreach (string element in elements)
        {
            if (element == "Array") continue;

            var m = arrayElem.Match(element);
            if (m.Success)
            {
                int index = int.Parse(m.Groups[1].Value);
                if (obj is IList list && index < list.Count) obj = list[index];
                else return null;
            }
            else
            {
                obj = ReflectionHelper.GetFieldValue(obj, element);
            }
            if (obj == null) break;
        }
        return obj;
    }

    private static bool EqualsRobust(object a, object b)
    {
        if (a == null || b == null) return a == b;

        if (a is Enum && IsNumeric(b)) a = Convert.ToInt64(a);
        else if (b is Enum && IsNumeric(a)) b = Convert.ToInt64(b);

        if (a is UnityEngine.Object ao && b is UnityEngine.Object bo) return ao == bo;

        return a.Equals(b);
    }

    private static bool IsNumeric(object o) =>
        o is byte or sbyte or short or ushort or int or uint or long or ulong
        or float or double or decimal;
}
#endif
