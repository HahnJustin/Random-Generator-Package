#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;   // TileType enum
using System;
using System.Collections.Generic;
using System.Linq;

[CustomPropertyDrawer(typeof(TileTypeCollisionAttribute))]
public class TileTypeCollisionDrawer : PropertyDrawer
{
    /* ÑüÑüÑüÑüÑü Static lookup table: value Å® enum-name ÑüÑüÑüÑüÑü */
    private static readonly Dictionary<int, string> TileTypeByValue =
        Enum.GetValues(typeof(TileType))
            .Cast<TileType>()
            .GroupBy(t => (int)t)                     // handle duplicates Å® comma-sep names
            .ToDictionary(g => g.Key,
                          g => string.Join(", ", g.Select(t => t.ToString())));

    /* ÑüÑüÑüÑüÑü GUI ÑüÑüÑüÑüÑü */
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        if (TryGetCollision(property.intValue, out string hitName))
        {
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            Rect help = new(position.x,
                             position.y + line + gap,
                             position.width,
                             line * 2);

            EditorGUI.HelpBox(help,
                $"ID {property.intValue} already maps to TileType: {hitName}",
                MessageType.Warning);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUI.GetPropertyHeight(property, label, true);
        return TryGetCollision(property.intValue, out _)
             ? h + (EditorGUIUtility.singleLineHeight * 2)  // help-box height
               + EditorGUIUtility.standardVerticalSpacing
             : h;
    }

    /* ÑüÑüÑüÑüÑü Helpers ÑüÑüÑüÑüÑü */
    private static bool TryGetCollision(int value, out string enumName) =>
        TileTypeByValue.TryGetValue(value, out enumName);
}
#endif
