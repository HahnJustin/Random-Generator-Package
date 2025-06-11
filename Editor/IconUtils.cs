// IconUtils.cs  (Editor folder)
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class IconUtils
{
    /// Returns the first icon whose name exists, or null.
    public static GUIContent Find(params string[] names)
    {
        foreach (string n in names)
        {
            var c = EditorGUIUtility.IconContent(n);
            if (c != null && c.image != null) return c;
        }
        return null;
    }
}
#endif