using System;
using System.Reflection;
using UnityEngine;

public static class ReflectionHelper
{
    public static object GetFieldValue(object obj, string fieldName)
    {
        Type type = obj.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            return field.GetValue(obj);
        }

        PropertyInfo property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null)
        {
            return property.GetValue(obj);
        }

        Debug.LogWarning($"Field or property '{fieldName}' not found in {obj.GetType().Name}");
        return null;
    }

    public static void SetFieldValue(object obj, string fieldName, object value)
    {
        Type type = obj.GetType();
        FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }

        PropertyInfo property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null)
        {
            property.SetValue(obj, value);
            return;
        }

        Debug.LogWarning($"Field or property '{fieldName}' not found in {obj.GetType().Name}");
    }
}