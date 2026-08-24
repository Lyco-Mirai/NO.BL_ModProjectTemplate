using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // BeginProperty keeps the tooltip intact
        label = EditorGUI.BeginProperty(position, label, property);

        GUI.enabled = false; // Grey out the field
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true; // Re-enable for other GUI elements

        EditorGUI.EndProperty();
    }
}
