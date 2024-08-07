using UnityEngine;
using UnityEditor;
using Dalichrome.RandomGenerator.Configs;

[CustomPropertyDrawer(typeof(ConditionAttribute))]
public class ConditionAttributeDrawer : PropertyDrawer
{
    private bool ShouldDisplay(SerializedProperty property, object objectToEqual)
    {
        bool shouldDisplay = false;

        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                shouldDisplay = property.boolValue.Equals(objectToEqual);
                break;
            case SerializedPropertyType.Enum:
                shouldDisplay = property.enumValueIndex.Equals(objectToEqual);
                break;
            case SerializedPropertyType.Float:
                shouldDisplay = property.floatValue.Equals(objectToEqual);
                break;
            case SerializedPropertyType.Integer:
                shouldDisplay = property.intValue.Equals(objectToEqual);
                break;
            case SerializedPropertyType.String:
                shouldDisplay = property.stringValue.Equals(objectToEqual);
                break;
            case SerializedPropertyType.Vector2:
                shouldDisplay = property.vector2Value.Equals(objectToEqual);
                break;
            //More cases to add
        }

        return shouldDisplay;
    }

    /*
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)

    {
        ConditionAttribute conditionAttribute = (ConditionAttribute)attribute;
        object targetObject = property.serializedObject.targetObject;
        object conditionValue = ReflectionHelper.GetFieldValue(targetObject, conditionAttribute.dependentVariable);

        if (conditionValue != null && ShouldDisplay(property, conditionAttribute.objectToEqual))
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionAttribute conditionAttribute = (ConditionAttribute)attribute;
        object targetObject = property.serializedObject.targetObject;
        object conditionValue = ReflectionHelper.GetFieldValue(targetObject, conditionAttribute.dependentVariable);

        if (conditionValue != null && ShouldDisplay(property, conditionAttribute.objectToEqual))
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        else
        {
            return 0;
        }
    }
    */
}
